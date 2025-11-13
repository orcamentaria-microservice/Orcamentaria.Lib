using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Responses;
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

        public async Task<HttpResponse<Response<T>>> SendAsync<T>(
            string baseUrl,
            EndpointRequest endpoint,
            OptionsRequest? options = null)
        {
            var requestContext = GetContext();

            if (options is null)
                options = new OptionsRequest();

            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Parse(endpoint.Method.ToUpper()),
                RequestUri = new Uri($"{baseUrl}{endpoint.Route}"),
            };

            if (requestContext is not null)
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

            HttpResponseMessage response = null;
            TimeSpan responseTime = TimeSpan.FromMilliseconds(0);
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                response = await _httpClient.SendAsync(requestMessage);
                responseTime = watch.Elapsed;

                response.EnsureSuccessStatusCode();

                return new HttpResponse<Response<T>>
                {
                    HttpResponseMessage = response,
                    Endpoint = endpoint,
                    ResponseTime = responseTime,
                    Content = JsonSerializer.Deserialize<Response<T>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                };
            }
            catch (HttpRequestException ex)
            {
                ResolveErrorMessage(ex);

                return new HttpResponse<Response<T>>
                {
                    HttpResponseMessage = response,
                    Endpoint = endpoint,
                    ResponseTime = responseTime,
                    Content = JsonSerializer.Deserialize<Response<T>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }),
                };
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
            finally
            {

            }
        }

        private void ResolveErrorMessage(HttpRequestException ex)
        {
            if (ex.StatusCode is null && ex.HttpRequestError == HttpRequestError.ConnectionError)
                throw new ServiceUnavailableException("O serviço está não está disponivel.");

            var messageUnavailableError = ex.StatusCode switch
            {
                HttpStatusCode.ServiceUnavailable => "O serviço está não está disponivel.",
                _ => ""
            };

            if (!string.IsNullOrEmpty(messageUnavailableError))
                throw new ServiceUnavailableException("O serviço está não está disponivel.");


            var messageRequestError = ex.StatusCode switch
            {
                HttpStatusCode.RequestTimeout => "O recurso excedeu o tempo de resposta.",
                HttpStatusCode.TooManyRequests => "Muitos requests ao recurso.",
                HttpStatusCode.ServiceUnavailable => "O recurso não esta disponivel",
                HttpStatusCode.GatewayTimeout => "O recurso downstream excedeu o tempo de resposta.",
                HttpStatusCode.NotImplemented => "O recurso não implementado.",
                HttpStatusCode.BadRequest => "Requisicao invalida. Valide os parametros (Params) e conteudo (Content) enviado.",
                HttpStatusCode.UnsupportedMediaType => "Requisicao invalida. Valide os parametros (Params) e conteudo (Content) enviado.",
                _ => ""
            };

            if (!string.IsNullOrEmpty(messageRequestError))
                throw new IntegrationException(messageRequestError, (HttpStatusCode)ex.StatusCode);

            var messageUnauthorizedError = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Usuario nao autorizado.",
                HttpStatusCode.Forbidden => "Usuario nao tem permissao para usar esse recurso.",
                _ => ""
            };

            if (!string.IsNullOrEmpty(messageUnauthorizedError))
                throw new UnauthorizedException(messageUnauthorizedError);
        }

        private IRequestContext GetContext()
        {
            try
            {
                return _httpContextAccessor.HttpContext?.RequestServices.GetService<IRequestContext>();
            }
            catch (Exception ex)
            {
                throw new UnexpectedException($"Erro inesperado ao capturar o RequestContext {ex.Message}", ex);
            }
        }
    }
}
