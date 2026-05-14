#if NET5_0_OR_GREATER

using System;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DiagnosticExplorer
{
    public static class DiagnosticHostingExtensions
    {
        public static IServiceCollection AddDiagnosticExplorer(
            this IServiceCollection services,
            IConfiguration config = null,
            Action<HttpConnectionOptions> configureHttp = null)
        {
            if (config != null)
            {
                services.Configure<DiagnosticOptions>(config.GetSection("DiagnosticExplorer"));
            }
            else
            {
                using (ServiceProvider provider = services.BuildServiceProvider())
                {
                    IConfiguration configuration = provider.GetService<IConfiguration>();
                    services.Configure<DiagnosticOptions>(configuration.GetSection("DiagnosticExplorer"));
                }
            }

            services.AddHostedService(sp =>
                new DiagnosticHostingService(sp.GetService<IOptions<DiagnosticOptions>>(), configureHttp));
            return services;
        }
    }
}

#endif
