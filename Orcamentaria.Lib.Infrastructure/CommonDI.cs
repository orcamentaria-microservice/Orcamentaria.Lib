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
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

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

            services.Configure<ServiceRegistryConfiguration>(configuration.GetSection("ServiceRegistry"));
            services.Configure<AuthenticationSecretsConfigurations>(configuration.GetSection("Secrets"));
            
            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
            services.AddSingleton<IServiceRegistryService, ServiceRegistryService>();
            services.AddSingleton<IRsaService, RsaService>();

            services.AddHostedService<ServiceRegistryHostedService>();

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
            if (env.IsDevelopment())
            {
                app.UseSwagger();
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

        private static string FormatServiceName(string serviceName) => $"{serviceName}.API";
    }
}
