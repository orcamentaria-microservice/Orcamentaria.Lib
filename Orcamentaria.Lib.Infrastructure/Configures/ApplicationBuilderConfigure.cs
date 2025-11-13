using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Infrastructure.Helpers;
using Orcamentaria.Lib.Infrastructure.Middlewares;

namespace Orcamentaria.Lib.Infrastructure.Configures
{
    public static class ApplicationBuilderConfigure
    {
        readonly static string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        public static void ConfigureCommon(
            this IApplicationBuilder app,
            IWebHostEnvironment env,
            string serviceName,
            string apiVersion)
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
                app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", $"{ServiceNameHelper.FormatServiceName(serviceName)} {apiVersion}"));
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors(MyAllowSpecificOrigins);


            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseMiddleware<AuthMiddleware>();
            app.UseMiddleware<RequestMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
