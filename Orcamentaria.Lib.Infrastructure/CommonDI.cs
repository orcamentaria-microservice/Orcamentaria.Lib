using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Orcamentaria.Lib.Application.HostedService;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Application.Providers;
using Orcamentaria.Lib.Domain.Providers;
using Microsoft.AspNetCore.Builder;
using Orcamentaria.Lib.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Orcamentaria.Lib.Domain.Services;
using Orcamentaria.Lib.Application.Services;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Infrastructure.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace Orcamentaria.Lib.Infrastructure
{
    public static class CommonDI
    {
        readonly static string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        public static void ResolveCommonServices(
            string serviceName,
            string apiVersion,
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.AddHttpClient();

            services.AddControllers()
                .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Token de acesso: Bearer {seu-token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });

                c.SwaggerDoc(apiVersion, new OpenApiInfo { Title = FormatServiceName(serviceName), Version = apiVersion });
            });

            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressConsumesConstraintForFormFileParameters = false;
                    options.SuppressInferBindingSourcesForParameters = true;
                    options.SuppressModelStateInvalidFilter = false;
                    options.SuppressMapClientErrors = false;
                    options.ClientErrorMapping[StatusCodes.Status404NotFound].Link =
                        "https://httpstatuses.com/404";
                });

            services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    });
            });

            services.AddScoped<IUserAuthContext, UserAuthContext>();

            if (configuration.GetSection("ServiceRegistryConfiguration") is null)
                throw new Exception("Serviço Registry não configurado.");

            services.Configure<ServiceRegistryConfiguration>(configuration.GetSection("ServiceRegistryConfiguration"));
            services.Configure<ApiGetawayConfiguration>(configuration.GetSection("ApiGetawayConfiguration"));

            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
            services.AddSingleton<IServiceRegistryService, ServiceRegistryService>();
            services.AddSingleton<IRsaService, RsaService>();
            services.AddSingleton<IHttpClientService, HttpClientService>();
            services.AddSingleton<IApiGetawayService, ApiGetawayService>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var sp = services.BuildServiceProvider();
                var rsaService = sp.GetRequiredService<IRsaService>();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "orcamentaria.auth",
                    ValidAudience = "orcamentaria.user",
                    IssuerSigningKey = rsaService.GenerateRsaSecurityKey(FormatServiceName(serviceName), "public_key_user.pem")
                };
            });
        }

        public static void ConfigureCommon(
            string serviceName,
            string apiVersion,
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value;
                var isSwaggerJson = path != null && path.Contains("/swagger/v1/swagger.json");

                if (isSwaggerJson)
                {
                    var serviceConfiguration = context.RequestServices.GetRequiredService<IOptions<ServiceConfiguration>>().Value;
                    var clientId = context.Request.Headers["ClientId"].ToString();
                    var clientSecret = context.Request.Headers["ClientSecret"].ToString();

                    if (!clientId.Equals(serviceConfiguration.ClientId, StringComparison.OrdinalIgnoreCase) ||
                    !clientSecret.Equals(serviceConfiguration.ClientSecret, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Unauthorized to access Swagger JSON");
                        return;
                    }
                }

                await next();
            });

            app.UseSwagger();

            if (env.IsDevelopment())
            {
                app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", $"{FormatServiceName(serviceName)} {apiVersion}"));
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors(MyAllowSpecificOrigins);

            app.UseAuthentication();
            app.UseMiddleware<UserAuthMiddleware>();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public static void AddServiceRegistryHosted(
            IServiceCollection services,
            IConfiguration configuration)
        {
            if (configuration.GetSection("ServiceConfiguration") is null)
                throw new Exception("Serviço não configurado.");

            services.Configure<ServiceConfiguration>(configuration.GetSection("ServiceConfiguration"));
            services.AddHostedService<ServiceRegistryHostedService>();
        }

        private static string FormatServiceName(string serviceName) => $"{serviceName}.API";
    }
}
