using Orcamentaria.Lib.Domain.Models;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Net;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;

        public HttpClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponse<T>> SendAsync<T>(
            string baseUrl, 
            EndpointRequest endpoint, 
            string? tokenAuth = null,
            object? content = null)
        {
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Parse(endpoint.Method.ToUpper()),
                RequestUri = new Uri($"{baseUrl}{endpoint.Route}"),
            };

            if(!String.IsNullOrEmpty(tokenAuth))
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenAuth}");

            if (content is not null)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                requestMessage.Content = new StringContent(JsonSerializer.Serialize(content, options), Encoding.UTF8, "application/json");
            }

            try
            {
                var stopWatch = Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(requestMessage);
                var responseTime = stopWatch.Elapsed;

                var error = ResolveResponseError(response);

                if (error is not null)
                    return new HttpResponse<T>
                    {
                        Success = false,
                        StatusCode = error.First().Key,
                        HttpResponseMessage = response,
                        Endpoint = endpoint,
                        ResponseTime = responseTime,
                        MessageError = error.First().Value
                    };

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return new HttpResponse<T>
                {
                    Success = true,
                    HttpResponseMessage = response,
                    Endpoint = endpoint,
                    ResponseTime = responseTime,
                    Content = JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), options),
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse<T>
                {
                    Success = false,
                    Endpoint = endpoint,
                    MessageError = ex.Message
                };
            }
        }

        private IDictionary<int, string>? ResolveResponseError(HttpResponseMessage responseMessage)
        {
            var statusCode = responseMessage.StatusCode;

            if (statusCode == HttpStatusCode.OK)
                return null;

            var messageError = responseMessage.StatusCode switch
            {
                HttpStatusCode.NotFound => "Recurso não encontrado.",
                HttpStatusCode.Unauthorized => "Usuário não autorizado.",
                HttpStatusCode.Forbidden => "Usuário não autorizado.",
                HttpStatusCode.InternalServerError => "Erro interno no recurso.",
                _ => "Erro não qualificado."
            };

            return new Dictionary<int, string> { { (int)responseMessage.StatusCode, messageError } };
        }
    }
}
