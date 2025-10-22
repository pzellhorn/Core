using System.Net.Http.Json;
using System.Text.Json;

namespace pzellhorn.Core.ClientAPI.Base
{
    public class ApiTransport(HttpClient http)
    {
        public JsonSerializerOptions JsonOptions { get; } = new() { PropertyNameCaseInsensitive = true };

        public async Task<TResult> Get<TResult>(string url, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, cancellationToken) ?? throw new InvalidOperationException("Empty response body.");
        }

        public async Task<TResult> Post<TResult>(string url, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, cancellationToken) ?? throw new InvalidOperationException("Empty response body.");
        }

        public async Task<TResult> Post<TRequest, TResult>(string url, TRequest body, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await http.PostAsJsonAsync(url, body, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, cancellationToken) ?? throw new InvalidOperationException("Empty response body.");
        }

        public Task<HttpResponseMessage> Delete(string url, CancellationToken cancellationToken = default) => http.DeleteAsync(url, cancellationToken);
    }
}