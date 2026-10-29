using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.PushNotifications
{
    /// <summary>
    /// Firebase Cloud Messaging, HTTP v1 API (docs/36). Signs in with a Google service account (the JSON key file that
    /// lives only on the CT, HADAL_FCM_SERVICE_ACCOUNT_JSON_PATH) through the OAuth 2 JWT-bearer flow, keeps the
    /// access token until shortly before it expires and posts one message per device token.
    /// </summary>
    public sealed class FcmPushSender : IPushSender, IDisposable
    {
        private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly string _projectId;
        private readonly string _clientEmail;
        private readonly string _tokenUri;
        private readonly RSA _key;
        private readonly SemaphoreSlim _tokenGate = new(1, 1);
        private string? _accessToken;
        private DateTimeOffset _accessTokenExpires;

        private FcmPushSender(string projectId, string clientEmail, string tokenUri, RSA key, HttpClient http, ILogger logger)
        {
            _projectId = projectId;
            _clientEmail = clientEmail;
            _tokenUri = tokenUri;
            _key = key;
            _http = http;
            _logger = logger;
        }

        public bool Enabled => true;

        /// <summary>
        /// The sender from a service-account key file and a Firebase project id; null (push stays off) when either is
        /// missing or the file is not a service-account key. The project id falls back to the key's own project_id.
        /// </summary>
        public static FcmPushSender? TryCreate(string? serviceAccountPath, string? projectId, ILogger logger, HttpClient? http = null)
        {
            if (string.IsNullOrWhiteSpace(serviceAccountPath) || !File.Exists(serviceAccountPath))
                return null;
            try
            {
                var json = JsonNode.Parse(File.ReadAllText(serviceAccountPath))?.AsObject();
                var email = json?["client_email"]?.GetValue<string>();
                var pem = json?["private_key"]?.GetValue<string>();
                var tokenUri = json?["token_uri"]?.GetValue<string>() ?? "https://oauth2.googleapis.com/token";
                var project = string.IsNullOrWhiteSpace(projectId) ? json?["project_id"]?.GetValue<string>() : projectId;
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pem) || string.IsNullOrWhiteSpace(project))
                {
                    logger.LogWarning("Push: {Path} is not a service-account key with a project; push stays off.", serviceAccountPath);
                    return null;
                }

                var key = RSA.Create();
                key.ImportFromPem(pem);
                return new FcmPushSender(project!.Trim(), email!, tokenUri, key, http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) }, logger);
            }
            catch (Exception exception) when (exception is JsonException or CryptographicException or ArgumentException or InvalidOperationException or IOException)
            {
                logger.LogWarning(exception, "Push: reading the service-account key failed; push stays off.");
                return null;
            }
        }

        public async Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken)
        {
            string token;
            try
            {
                token = await AccessTokenAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
            {
                _logger.LogWarning(exception, "Push: Google sign-in failed.");
                return PushSendResult.Failed;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send")
            {
                Content = new StringContent(Body(message).ToJsonString(), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                using var response = await _http.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return PushSendResult.Sent;
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound || text.Contains("UNREGISTERED", StringComparison.Ordinal)
                    || (response.StatusCode == HttpStatusCode.BadRequest && text.Contains("registration token", StringComparison.OrdinalIgnoreCase)))
                    return PushSendResult.InvalidToken;
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    _accessToken = null;
                _logger.LogWarning("Push: FCM answered {Status}: {Text}", (int)response.StatusCode, text.Length > 300 ? text[..300] : text);
                return PushSendResult.Failed;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(exception, "Push: FCM request failed.");
                return PushSendResult.Failed;
            }
        }

        /// <summary>The FCM v1 message: notification, Android channel and small icon, the screen to open in data.</summary>
        public static JsonObject Body(PushMessage message) => new()
        {
            ["message"] = new JsonObject
            {
                ["token"] = message.Token,
                ["notification"] = new JsonObject { ["title"] = message.Title, ["body"] = message.Body },
                ["data"] = new JsonObject { ["screen"] = message.Screen },
                ["android"] = new JsonObject
                {
                    ["priority"] = "high",
                    ["notification"] = new JsonObject
                    {
                        ["channel_id"] = message.ChannelId,
                        ["icon"] = HADAL.Shared.Simulation.NotificationRules.SmallIcon,
                        ["tag"] = message.Tag
                    }
                }
            }
        };

        private async Task<string> AccessTokenAsync(CancellationToken cancellationToken)
        {
            if (_accessToken != null && DateTimeOffset.UtcNow < _accessTokenExpires)
                return _accessToken;
            await _tokenGate.WaitAsync(cancellationToken);
            try
            {
                if (_accessToken != null && DateTimeOffset.UtcNow < _accessTokenExpires)
                    return _accessToken;

                var now = DateTimeOffset.UtcNow;
                using var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = Assertion(now)
                });
                using var response = await _http.PostAsync(_tokenUri, form, cancellationToken);
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"token endpoint answered {(int)response.StatusCode}");
                var json = JsonNode.Parse(text)!.AsObject();
                _accessToken = json["access_token"]!.GetValue<string>();
                var seconds = json["expires_in"]?.GetValue<int>() ?? 3600;
                _accessTokenExpires = now.AddSeconds(Math.Max(60, seconds - 300));
                return _accessToken;
            }
            finally
            {
                _tokenGate.Release();
            }
        }

        /// <summary>The signed JWT (RS256) Google exchanges for an access token.</summary>
        internal string Assertion(DateTimeOffset now)
        {
            var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
            var claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
            {
                iss = _clientEmail,
                scope = Scope,
                aud = _tokenUri,
                iat = now.ToUnixTimeSeconds(),
                exp = now.AddMinutes(60).ToUnixTimeSeconds()
            }));
            var signed = header + "." + claims;
            var signature = _key.SignData(Encoding.ASCII.GetBytes(signed), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return signed + "." + Base64Url(signature);
        }

        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public void Dispose()
        {
            _key.Dispose();
            _tokenGate.Dispose();
        }
    }
}
