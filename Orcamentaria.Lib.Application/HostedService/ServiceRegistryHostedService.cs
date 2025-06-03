using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.DTOs;
using Orcamentaria.Lib.Domain.HostedService;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Services;
using System.Text.Json;

namespace Orcamentaria.Lib.Application.HostedService
{
    public class ServiceRegistryHostedService : IServiceRegistryHostedService, IHostedService
    {
        private readonly IServer _server;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IOptions<ServiceRegistryConfiguration> _serviceRegistryConfiguration;
        private readonly HttpClient _httpClient;
        private readonly IServiceRegistryService _serviceRegistryService;

        public ServiceRegistryHostedService(
            IServer server,
            IHostApplicationLifetime lifetime, 
            IOptions<ServiceRegistryConfiguration> serviceRegistryConfiguration, 
            HttpClient httpClient,
            IServiceRegistryService serviceRegistryService)
        {
            _server = server;
            _lifetime = lifetime;
            _serviceRegistryConfiguration = serviceRegistryConfiguration;
            _httpClient = httpClient;
            _serviceRegistryService = serviceRegistryService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lifetime.ApplicationStarted.Register(OnStarted);
            return Task.CompletedTask;
        }

        public async void SendHeartbeat(object state)
        {
            await _serviceRegistryService.SendAsync(
                    url: $"{_serviceRegistryConfiguration.Value.BaseUrl}{_serviceRegistryConfiguration.Value.HeartbeatRoute}",
                    content: null,
                    method: HttpMethod.Put
                    );
        }

        public async Task SendRegisterService()
        {
            var address = _server.Features.Get<IServerAddressesFeature>()?.Addresses?.FirstOrDefault();

            var swaggerUrl = $"{address}swagger/v1/swagger.json";

            var endpoints = await GetEndpointsFromSwaggerAsync(swaggerUrl);

            var payload = new ServiceRegistryInsertDTO
            {
                Name = _serviceRegistryConfiguration.Value.ServiceName,
                BaseUrl = address!,
                Endpoints = endpoints
            };

            try
            {
                var requestFailed = false;
                var attemptNumber = 1;

                do
                {
                    var result = await _serviceRegistryService.SendAsync(
                        url: $"{_serviceRegistryConfiguration.Value.BaseUrl}{_serviceRegistryConfiguration.Value.RegistryServiceRoute}",
                        content: payload,
                        method: HttpMethod.Post);

                    if (result.Success)
                    {
                        requestFailed = false;
                        break;
                    }

                    requestFailed = true;
                    attemptNumber++;

                    await Task.Delay(TimeSpan.FromSeconds(30));

                } while (attemptNumber <= 6);

                if (requestFailed)
                {
                    //Fazer log para salvar erro das tentantivas
                }

            }
            catch (Exception ex)
            {

            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async void OnStarted()
        {
            await SendRegisterService();

            new Timer(SendHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private async Task<List<ServiceRegistryEndpoinsInsertDTO>> GetEndpointsFromSwaggerAsync(string swaggerUrl)
        {
            var endpoints = new List<ServiceRegistryEndpoinsInsertDTO>();

            var response = await _httpClient.GetAsync(swaggerUrl);
            response.EnsureSuccessStatusCode();

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
    }
}
