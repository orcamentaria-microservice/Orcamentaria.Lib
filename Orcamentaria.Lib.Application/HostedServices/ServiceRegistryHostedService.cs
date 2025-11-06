 using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Application.Services;
using Orcamentaria.Lib.Domain.DTOs.ServiceRegistry;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.HostedServices;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Exceptions;
using Orcamentaria.Lib.Domain.Models.Logs;
using Orcamentaria.Lib.Domain.Services;
using System.Net;
using System.Text.Json;

namespace Orcamentaria.Lib.Application.HostedServices
{
    public class ServiceRegistryHostedService : IServiceRegistryHostedService, IHostedService
    {
        private readonly IServiceRegistryService _serviceRegistryService;
        private readonly IMemoryCacheService _memoryCacheService;
        private readonly ILogService _logService;
        private readonly ApiGetawayConfiguration _apiGetawayConfiguration;
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly IServer _server;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly HttpClient _httpClient;

        public ServiceRegistryHostedService(
            IMemoryCacheService memoryCacheService,
            ILogService logService,
            IServiceRegistryService serviceRegistryService,
            IOptions<ServiceConfiguration> serviceConfiguration,
            IOptions<ApiGetawayConfiguration> apiGetawayConfiguration,
            IServer server,
            IHostApplicationLifetime lifetime, 
            HttpClient httpClient)
        {
            _server = server;
            _lifetime = lifetime;
            _apiGetawayConfiguration = apiGetawayConfiguration.Value;
            _httpClient = httpClient;
            _serviceRegistryService = serviceRegistryService;
            _serviceConfiguration = serviceConfiguration.Value;
            _memoryCacheService = memoryCacheService;
            _logService = logService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lifetime.ApplicationStarted.Register(OnStarted);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => throw new TaskCanceledException("Ocorreu um erro na registro do serviço.");

        public async Task SendRegisterService()
        {
            try
            {
                var address = _server.Features.Get<IServerAddressesFeature>()?.Addresses?.FirstOrDefault();

                if (String.IsNullOrEmpty(address))
                    throw new IntegrationException("Não foi possível obter a URL do swagger. Não é possivel iniciar serviço.", HttpStatusCode.NotFound);

                if (_apiGetawayConfiguration is null)
                    throw new ConfigurationException("Service Registry não configurado. Não é possivel iniciar serviço.", ErrorCodeEnum.NotFound);

                if (string.IsNullOrEmpty(_apiGetawayConfiguration.BaseUrl))
                    throw new ConfigurationException("BaseUrl do Service Registry não configurada. Não é possivel iniciar serviço.", ErrorCodeEnum.NotFound);

                var swaggerUrl = $"{address}swagger/v1/swagger.json";

                var endpoints = await GetEndpointsFromSwaggerAsync(swaggerUrl);

                var payload = new ServiceRegistryInsertDTO
                {
                    Name = _serviceConfiguration.ServiceName,
                    BaseUrl = address!,
                    Endpoints = endpoints
                };

                var requestFailed = false;
                var attemptNumber = 1;
                HttpStatusCode errorCode = HttpStatusCode.InternalServerError;

                do
                {
                    var result = await _serviceRegistryService.Register(payload);

                    if (result.Success)
                    {
                        requestFailed = false;
                        _memoryCacheService.SetMemoryCache($"{_serviceConfiguration.ServiceName}_key", result.Data);
                        break;
                    }

                    requestFailed = true;
                    errorCode = (HttpStatusCode)result.Error.ErrorCode;
                    attemptNumber++;

                    await Task.Delay(TimeSpan.FromSeconds(30));

                } while (attemptNumber <= 6);

                if (requestFailed)
                    throw new IntegrationException($"O serviço {_serviceConfiguration.ServiceName} não conseguiu se registrar no Service Registry. Não é possivel iniciar serviço.", errorCode);
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        public async Task SendHeartbeat()
        {
            try
            {
                if (!_memoryCacheService.GetMemoryCache($"{_serviceConfiguration.ServiceName}_key", out string serviceId))
                    throw new BusinessException("Falha para obter o ID do serviço. Não é foi enviar o heartbeat ao Service Registry", ErrorCodeEnum.NotFound);

                while (true)
                {
                    var response = await _serviceRegistryService.Heartbeat(serviceId);

                    if (!response.Success)
                    {
                        if(response.Error.ErrorCode == ErrorCodeEnum.Unauthorized)
                            response = await _serviceRegistryService.Heartbeat(serviceId, true);
                        else
                            throw new IntegrationException("Falha ao enviar heartbeat para o Service Registry.", (HttpStatusCode)response.Error.ErrorCode);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        #region Private Methods
        private async void OnStarted()
        {
            try
            {
                await SendRegisterService();

                SendHeartbeat();
            }
            catch (DefaultException ex)
            {
                var origin = new ServiceExceptionOrigin
                {
                    Type = OriginEnum.Internal,
                    ProcessName = "HostedService"
                };

                await _logService.ResolveLogAsync(ex, origin);

                StopAsync(CancellationToken.None).Wait();
            }
        }

        private async Task<List<ServiceRegistryEndpoinsInsertDTO>> GetEndpointsFromSwaggerAsync(string swaggerUrl)
        {
            try
            {
                var endpoints = new List<ServiceRegistryEndpoinsInsertDTO>();

                _httpClient.DefaultRequestHeaders.Add("ClientId", _serviceConfiguration.ClientId);
                _httpClient.DefaultRequestHeaders.Add("ClientSecret", _serviceConfiguration.ClientSecret);

                var response = await _httpClient.GetAsync(swaggerUrl);
                response.EnsureSuccessStatusCode();

                _httpClient.Dispose();

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var paths = root.GetProperty("paths");

                foreach (var path in paths.EnumerateObject())
                {
                    foreach (var method in path.Value.EnumerateObject())
                    {
                        var route = path.Name.TrimStart('/');
                        var methodName = method.Name.ToUpperInvariant();

                        method.Value.TryGetProperty("operationId", out var operationId);

                        endpoints.Add(new ServiceRegistryEndpoinsInsertDTO
                        {
                            Name = operationId.ToString(),
                            Method = methodName,
                            Route = route
                        });
                    }
                }

                return endpoints;
            }
            catch (HttpRequestException ex)
            {
                throw new IntegrationException("Não foi possivel conectar no swagger do serviço", ex.StatusCode ?? HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        #endregion
    }
}
