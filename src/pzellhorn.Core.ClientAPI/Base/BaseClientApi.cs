using System.Net;
using System.Text.Json;

namespace pzellhorn.Core.ClientAPI.Base
{
    public abstract class BaseClientApi<TReq, TRes>(ApiTransport api, string controllerSegment) : IBaseClientApi<TReq, TRes>
    {
        protected virtual string BasePath => $"api/{controllerSegment}";
        protected virtual JsonSerializerOptions Json => api.JsonOptions;

        protected virtual string ActionUrl(string action, params (string Key, string? Value)[] query)
        {
            string baseUrl = $"{BasePath}/{action}";
            if (query is null || query.Length == 0) return baseUrl;

            string queryString = string.Join("&", query.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                     .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

            return string.IsNullOrEmpty(queryString) ? baseUrl : $"{baseUrl}?{queryString}";
        }

        public Task<TRes?> Get(Guid id, CancellationToken cancellationToken = default)
           => api.Get<TRes?>(ActionUrl(nameof(Get), ("id", id.ToString())), cancellationToken);

        public Task<List<TRes>> GetFor(string key, string propertyName, CancellationToken cancellationToken = default)
            => api.Get<List<TRes>>(ActionUrl(nameof(GetFor), ("key", key), ("propertyName", propertyName)), cancellationToken);

        public Task<TRes> Upsert(TReq request, CancellationToken cancellationToken = default)
            => api.Post<TReq, TRes>(ActionUrl(nameof(Upsert)), request, cancellationToken);

        public Task<List<TRes>> UpsertMany(List<TReq> requests, CancellationToken cancellationToken = default)
            => api.Post<List<TReq>, List<TRes>>(ActionUrl(nameof(UpsertMany)), requests, cancellationToken);

        public async Task<bool> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var resp = await api.Delete(ActionUrl(nameof(Delete), ("id", id.ToString())), cancellationToken);
            if (resp.StatusCode == HttpStatusCode.NoContent) return true;
            if (resp.StatusCode == HttpStatusCode.NotFound) return false;
            resp.EnsureSuccessStatusCode();
            return true;
        }

        protected Task<TResult> Post<TResult>(string action, CancellationToken cancellationToken = default, params (string Key, string? Value)[] query)
            => api.Post<TResult>(ActionUrl(action, query), cancellationToken);

        protected Task<TResult> Post<TRequest, TResult>(string action, TRequest body, CancellationToken cancellationToken = default, params (string Key, string? Value)[] query)
           => api.Post<TRequest, TResult>(ActionUrl(action, query), body, cancellationToken);
    }
}
