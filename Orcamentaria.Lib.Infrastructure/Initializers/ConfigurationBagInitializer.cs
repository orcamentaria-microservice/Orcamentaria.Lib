using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Application.Providers;
using Orcamentaria.Lib.Application.Services;
using Orcamentaria.Lib.Domain.DTOs.ConfigurationBag;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using System.Xml.Linq;

namespace Orcamentaria.Lib.Infrastructure.Initializers
{
    public class ConfigurationBagInitializer
    {
        private readonly string _serviceName;

        public ConfigurationBagInitializer(string serviceName)
        {
            _serviceName = serviceName;
        }

        public async Task<IConfigurationRoot> InitializeAsync(IConfiguration configuration)
        {
            try
            {
                var httpClient = new HttpClient();
                var httpContextAccessor = new HttpContextAccessor();
                var httpClientService = new HttpClientService(httpClient, httpContextAccessor);

                var apiGetawayConfiguration = configuration.GetSection("ApiGetawayConfiguration").Get<ApiGetawayConfiguration>();

                if (apiGetawayConfiguration is null)
                    throw new ConfigurationException("API Getaway não configurado.");

                if (apiGetawayConfiguration.BaseUrl is null)
                    throw new ConfigurationException("BaseURL do API Getaway não configurado.");

                var options = Options.Create(apiGetawayConfiguration);

                var client = new ApiGetawayService(httpClientService, options);

                var bootstrapTokenProvider = new BootstrapTokenProvider(apiGetawayConfiguration.BaseUrl, client, configuration);

                var token = await bootstrapTokenProvider.GetTokenAsync();

                var resource = new ResourceConfiguration
                {
                    ServiceName = "ConfigBagService",
                    EndpointName = "ConfigurationBagGetByServiceName",
                };

                IDictionary<string, string> @params = new Dictionary<string, string>();

                @params.Add("serviceName", _serviceName);

                var response = await client.Routing<ConfigurationBagResponseDTO>(
                        apiGetawayConfiguration.BaseUrl,
                        resource.ServiceName,
                        resource.EndpointName,
                        token,
                        @params,
                        null);

                if (!response.Success)
                    throw new ConfigurationException($"Erro ao buscar configurações do serviço {_serviceName}");

                var dict = ToEnvsDictionary(response.Data);

                dict["ServiceConfiguration:ServiceName"] = _serviceName.Split(".").Last();

                var newConfig = new ConfigurationBuilder()
                    .AddConfiguration(configuration)
                    .AddInMemoryCollection(dict)
                    .Build();

                return newConfig;
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

        #region private methods
        private static Dictionary<string, string> ToEnvsDictionary(ConfigurationBagResponseDTO bag)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(bag.ServiceName))
                dict["ConfigurationBag:ServiceName"] = bag.ServiceName;
            dict["ConfigurationBag:UpdateAt"] = bag.UpdateAt.ToString("O");

            if (bag.ConnectionStrings is not null)
            {
                foreach (var row in bag.ConnectionStrings)
                {
                    if (row is null) continue;

                    if (row.TryGetValue("Name", out var name) &&
                        (row.TryGetValue("ConnectionString", out var cs) || row.TryGetValue("Value", out cs)))
                    {
                        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(cs))
                            dict[$"ConnectionStrings:{name}"] = cs;
                        continue;
                    }

                    foreach (var kv in row)
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                            dict[$"ConnectionStrings:{kv.Key}"] = kv.Value;
                    }
                }
            }
            if (bag.Configurations is not null)
            {
                foreach (var item in bag.Configurations)
                {
                    if (item is null) continue;

                    foreach (var (sectionName, sectionValue) in item)
                    {
                        FlattenObject(dict, sectionValue, sectionName);
                    }
                }
            }

            if (bag.RealTimeConfigurations is not null)
            {
                foreach (var row in bag.RealTimeConfigurations)
                {
                    if (row is null) continue;

                    var key = row.Keys.First();
                    var value = row.Values.First();

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        dict[$"RealTimeConfigurations:{key}"] = value;
                        continue;
                    }
                }
            }

            return dict;
        }

        private static void FlattenObject(Dictionary<string, string> bag, object? value, string prefix)
        {
            if (value is null) return;

            switch (value)
            {
                case string s:
                    bag[prefix] = s;
                    return;

                case bool b:
                    bag[prefix] = b.ToString();
                    return;

                case int or long or short or byte or double or float or decimal:
                    bag[prefix] = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
                    return;

                case DateTime dt:
                    bag[prefix] = dt.ToString("O");
                    return;

                case IDictionary<string, object> dictObj:
                    foreach (var (k, v) in dictObj)
                        FlattenObject(bag, v, $"{prefix}:{k}");
                    return;

                case IEnumerable<object> list:
                    int i = 0;
                    foreach (var item in list)
                        FlattenObject(bag, item, $"{prefix}:{i++}");
                    return;

                default:
                    var json = System.Text.Json.JsonSerializer.Serialize(value);
                    var el = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    FlattenJsonElement(bag, el, prefix);
                    return;
            }
        }

        private static void FlattenJsonElement(Dictionary<string, string> bag, System.Text.Json.JsonElement el, string prefix)
        {
            switch (el.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Object:
                    foreach (var p in el.EnumerateObject())
                        FlattenJsonElement(bag, p.Value, $"{prefix}:{p.Name}");
                    break;

                case System.Text.Json.JsonValueKind.Array:
                    int i = 0;
                    foreach (var item in el.EnumerateArray())
                        FlattenJsonElement(bag, item, $"{prefix}:{i++}");
                    break;

                case System.Text.Json.JsonValueKind.String:
                    bag[prefix] = el.GetString()!;
                    break;

                case System.Text.Json.JsonValueKind.Number:
                    bag[prefix] = el.GetRawText();
                    break;

                case System.Text.Json.JsonValueKind.True:
                case System.Text.Json.JsonValueKind.False:
                    bag[prefix] = el.GetBoolean().ToString();
                    break;
            }
        }

        #endregion
    }
}
