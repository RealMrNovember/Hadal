using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Hadal.Core.Network
{
    /// <summary>Thrown when the server could not be reached at all (as opposed to an HTTP error status).</summary>
    public sealed class ServerUnreachableException : Exception
    {
        public ServerUnreachableException(string message) : base(message) { }
    }

    /// <summary>
    /// Minimal JSON-over-HTTP for the Auth API and patch manifest (the only JSON on the wire).
    /// Awaitable on Unity's main-thread context; shared contracts use public fields for JsonUtility.
    /// </summary>
    public static class HttpJson
    {
        private const int TimeoutSeconds = 10;

        public static async Task<(long Status, TResponse Body)> PostAsync<TRequest, TResponse>(string url, TRequest body)
            where TResponse : class
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body))),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return await SendAsync<TResponse>(request);
        }

        public static async Task<(long Status, TResponse Body)> GetAsync<TResponse>(string url) where TResponse : class
        {
            using var request = UnityWebRequest.Get(url);
            request.timeout = TimeoutSeconds;
            return await SendAsync<TResponse>(request);
        }

        private static async Task<(long, TResponse)> SendAsync<TResponse>(UnityWebRequest request) where TResponse : class
        {
            var done = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => done.TrySetResult(true);
            await done.Task;

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.DataProcessingError)
                throw new ServerUnreachableException($"{request.method} {request.url}: {request.error}");

            var status = request.responseCode;
            if (status < 200 || status >= 300)
                return (status, null);

            var text = request.downloadHandler.text;
            return (status, string.IsNullOrEmpty(text) ? null : JsonUtility.FromJson<TResponse>(text));
        }
    }
}
