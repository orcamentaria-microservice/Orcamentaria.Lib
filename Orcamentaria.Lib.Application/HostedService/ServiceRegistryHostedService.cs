using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.DTOs;
using Orcamentaria.Lib.Domain.DTOs.ServiceRegistry;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.HostedService;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Exceptions;
using Orcamentaria.Lib.Domain.Services;
using System.Net;
using System.Text.Json;

namespace Orcamentaria.Lib.Application.HostedService
{
    public class ServiceRegistryHostedService : IServiceRegistryHostedService, IHostedService
    {
        private readonly IServer _server;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ServiceRegistryConfiguration _serviceRegistryConfiguration;
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly HttpClient _httpClient;
        private readonly IServiceRegistryService _serviceRegistryService;
        private ServiceRegistryConfigurationEndpoint _registerEndpoint;
        private ServiceRegistryConfigurationEndpoint _heartbeatEndpoint;
        private readonly IMemoryCacheService _memoryCacheService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceRegistryHostedService(
            IServer server,
            IHostApplicationLifetime lifetime, 
            IOptions<ServiceRegistryConfiguration> serviceRegistryConfiguration,
            HttpClient httpClient,
            IServiceRegistryService serviceRegistryService,
            IOptions<ServiceConfiguration> serviceConfiguration,
            IMemoryCacheService memoryCacheService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _server = server;
            _lifetime = lifetime;
            _serviceRegistryConfiguration = serviceRegistryConfiguration.Value;
            _httpClient = httpClient;
            _serviceRegistryService = serviceRegistryService;
            _serviceConfiguration = serviceConfiguration.Value;
            _memoryCacheService = memoryCacheService;
            _serviceScopeFactory = serviceScopeFactory;

            var endpoints = _serviceRegistryConfiguration.Endpoints;

            _registerEndpoint = endpoints.FirstOrDefault(x => x.Name.ToLower() == "register");
            _heartbeatEndpoint = endpoints.FirstOrDefault(x => x.Name.ToLower() == "heartbeat");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lifetime.ApplicationStarted.Register(OnStarted);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async void OnStarted()
        {
            try
            {
                await SendRegisterService();

                SendHeartbeat();
            }
            catch (DefaultException ex)
            {
                var scope = _serviceScopeFactory.CreateScope();
                var logExceptionService = scope.ServiceProvider.GetService<ILogExceptionService>();

                logExceptionService.ResolveLog(ex);
            }
        }

        public async Task SendRegisterService()
        {
            try
            {
                var address = _server.Features.Get<IServerAddressesFeature>()?.Addresses?.FirstOrDefault();

                if (String.IsNullOrEmpty(address))
                    throw new IntegrationException("Não foi possível obter a URL do swagger.", HttpStatusCode.NotFound);

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
                    var result = await _serviceRegistryService.SendServiceRegister<string>(
                        baseUrl: _serviceRegistryConfiguration.BaseUrl,
                        endpoint: _registerEndpoint,
                        content: payload);

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
                    throw new IntegrationException($"O serviço {_serviceConfiguration.ServiceName} não conseguiu se registrar no Service Registry.", errorCode);
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
                    throw new BusinessException("Falha para obter o ID do serviço. Não é possivel mandar o heartbeat ao Service Registry", ErrorCodeEnum.NotFound);

                _heartbeatEndpoint.Route = _heartbeatEndpoint.Route.Replace("{serviceId}", serviceId);

                while (true)
                {
                    await _serviceRegistryService.SendServiceRegister<Task>(
                            baseUrl: _serviceRegistryConfiguration.BaseUrl,
                            endpoint: _heartbeatEndpoint);

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
    }
}
