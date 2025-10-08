using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Exceptions;
using Orcamentaria.Lib.Domain.Services;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Orcamentaria.Lib.Application.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpClientService(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<HttpResponse<T>> SendAsync<T>(
            string baseUrl,
            EndpointRequest endpoint,
            OptionsRequest? options = null)
        {
            try
            {
                var requestContext = GetContext();

                if (options is null)
                    options = new OptionsRequest();

                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Parse(endpoint.Method.ToUpper()),
                    RequestUri = new Uri($"{baseUrl}{endpoint.Route}"),
                };

                if(requestContext is not null)
                {
                    requestMessage.Headers.Add("RequestId", requestContext.RequestId);
                    requestMessage.Headers.Add("RequestOrderId", requestContext.RequestOrderId.ToString());
                }

                if (!String.IsNullOrEmpty(options.TokenAuth))
                    requestMessage.Headers.Add("Authorization", $"Bearer {options.TokenAuth}");

                if (options.Content is not null)
                    requestMessage.Content = new StringContent(JsonSerializer.Serialize(options.Content, new JsonSerializerOptions
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

                if(error.ContainsKey(HttpStatusCode.ServiceUnavailable))
                    throw new IntegrationException(messageError, error.Keys.First());

                throw new InfoException(messageError, ErrorCodeEnum.ExternalServiceFailure);
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
                            HttpStatusCode.ServiceUnavailable,
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

        private IRequestContext GetContext()
        {
            return _httpContextAccessor.HttpContext?.RequestServices.GetService<IRequestContext>();
        }
    }
}
