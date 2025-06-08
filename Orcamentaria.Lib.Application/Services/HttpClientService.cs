using Orcamentaria.Lib.Domain.Models;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Net;
using Orcamentaria.Lib.Domain.Services;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Enums;

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
            try
            {
                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Parse(endpoint.Method.ToUpper()),
                    RequestUri = new Uri($"{baseUrl}{endpoint.Route}"),
                };

                _httpClient.DefaultRequestHeaders.Remove("Authorization");

                if (!String.IsNullOrEmpty(tokenAuth))
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenAuth}");

                if (content is not null)
                    requestMessage.Content = new StringContent(JsonSerializer.Serialize(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    }), Encoding.UTF8, "application/json");

                var stopWatch = Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(requestMessage);
                var responseTime = stopWatch.Elapsed;

                response.EnsureSuccessStatusCode();

                return new HttpResponse<T>
                {
                    HttpResponseMessage = response,
                    Endpoint = endpoint,
                    ResponseTime = responseTime,
                    Content = JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }),
                };
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                var error = ResolveResponseError(ex);

                var messageError = String.Format(error.Values.First(), baseUrl, endpoint.Route);

                throw new IntegrationException(messageError, error.Keys.First());
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);

            }
        }

        private IDictionary<HttpStatusCode, string> ResolveResponseError(HttpRequestException exception)
        {
            try
            {
                var defaultMessageError = " host: {0} - path: {1}";

                if (exception.StatusCode is null && exception.Message.Contains("Nenhuma conexão pôde ser feita"))
                    return new Dictionary<HttpStatusCode, string> {
                        {
                            HttpStatusCode.InternalServerError,
                            $"Não foi possivel se conectar ao serviço. {defaultMessageError}"
                        } };

                var statusCode = exception.StatusCode;

                var messageError = statusCode switch
                {
                    HttpStatusCode.NotFound => "Recurso não encontrado.",
                    HttpStatusCode.Unauthorized => "Usuário não autorizado.",
                    HttpStatusCode.Forbidden => "Usuário não autorizado.",
                    HttpStatusCode.InternalServerError => "Erro interno no recurso.",
                    _ => "Erro não qualificado."
                };

                messageError += defaultMessageError;

                return new Dictionary<HttpStatusCode, string> { { statusCode ?? HttpStatusCode.InternalServerError, messageError } };
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }
    }
}
