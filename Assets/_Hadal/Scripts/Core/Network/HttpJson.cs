using System;
using System.Text;
using System.Threading.Tasks;
using HADAL.Shared.Auth;
using UnityEngine;
using UnityEngine.Networking;

namespace Hadal.Core.Network
{
    /// <summary>Thrown when the server could not be reached at all (as opposed to an HTTP error status).</summary>
    public sealed class ServerUnreachableException : Exception
    {
        public ServerUnreachableException(string message) : base(message) { }
    }

    /// <summary>An HTTP answer: the body on 2xx, otherwise the account API's error code (if it sent one).</summary>
    public readonly struct HttpResult<T> where T : class
    {
        public HttpResult(long status, T body, string error)
        {
            Status = status;
            Body = body;
            Error = error;
        }

        public long Status { get; }
        public T Body { get; }

        /// <summary><see cref="AccountErrors"/> value, or empty when the server sent none.</summary>
        public string Error { get; }

        public bool IsSuccess => Status >= 200 && Status < 300;
    }

    /// <summary>
    /// Minimal JSON-over-HTTP for the Auth / Account API and patch manifest (the only JSON on the wire).
    /// Awaitable on Unity's main-thread context; shared contracts use public fields for JsonUtility.
    /// </summary>
    public static class HttpJson
    {
        private const int TimeoutSeconds = 10;

        public static async Task<(long Status, TResponse Body)> PostAsync<TRequest, TResponse>(string url, TRequest body)
            where TResponse : class
        {
            var result = await SendAsync<TResponse>(UnityWebRequest.kHttpVerbPOST, url, body, null);
            return (result.Status, result.Body);
        }

        public static async Task<(long Status, TResponse Body)> GetAsync<TResponse>(string url) where TResponse : class
        {
            var result = await SendAsync<TResponse>(UnityWebRequest.kHttpVerbGET, url, null, null);
            return (result.Status, result.Body);
        }

        /// <summary>GET a non-JSON body (the live catalog); null on any non-2xx answer.</summary>
        public static async Task<(long Status, string Text, string Header)> GetTextAsync(string url, string header)
        {
            using var request = UnityWebRequest.Get(url);
            request.timeout = TimeoutSeconds;

            var done = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => done.TrySetResult(true);
            await done.Task;

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.DataProcessingError)
                throw new ServerUnreachableException($"GET {url}: {request.error}");

            var ok = request.responseCode >= 200 && request.responseCode < 300;
            return (request.responseCode, ok ? request.downloadHandler.text : null, request.GetResponseHeader(header));
        }

        /// <summary>Any verb, optional JSON body and optional Bearer access token.</summary>
        public static async Task<HttpResult<TResponse>> SendAsync<TResponse>(string method, string url, object body, string bearer)
            where TResponse : class
        {
            using var request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds
            };

            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (!string.IsNullOrEmpty(bearer))
                request.SetRequestHeader("Authorization", "Bearer " + bearer);

            var done = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => done.TrySetResult(true);
            await done.Task;

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.DataProcessingError)
                throw new ServerUnreachableException($"{request.method} {request.url}: {request.error}");

            var status = request.responseCode;
            var text = request.downloadHandler.text;
            if (status >= 200 && status < 300)
                return new HttpResult<TResponse>(status, string.IsNullOrEmpty(text) ? null : JsonUtility.FromJson<TResponse>(text), string.Empty);

            return new HttpResult<TResponse>(status, null, ReadError(text));
        }

        private static string ReadError(string text)
        {
            if (string.IsNullOrEmpty(text) || text[0] != '{')
                return string.Empty;

            try
            {
                return JsonUtility.FromJson<AccountError>(text)?.error ?? string.Empty;
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }
    }
}
