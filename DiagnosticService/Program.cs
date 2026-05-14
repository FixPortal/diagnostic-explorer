using System.Configuration;
using System.Text.Json.Serialization;
using DiagnosticExplorer;
using DiagnosticExplorer.Common;
using Diagnostics.Service.Common.Hubs;
using Microsoft.Extensions.Options;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddWindowsService(options => {
            options.ServiceName = "DiagnosticExplorer";
        });

        builder.Configuration.AddJsonFile(Expand(@"Config\settings.json"));

        builder.Services.Configure<DiagServiceSettings>(builder.Configuration.GetSection(nameof(DiagServiceSettings)));
        builder.Services.AddDiagnosticExplorer(builder.Configuration);

        var services = builder.Services;

        services.AddCors(opt => {
            opt.AddPolicy("CorsPolicy", builder => {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        services.AddSignalR();

        services.AddSingleton<RealtimeManager>();
        services.AddSingleton<RetroManager>();
        services.AddSignalR().AddHubOptions<DiagnosticHub>(options => {
            options.MaximumReceiveMessageSize = int.MaxValue;
            options.MaximumParallelInvocationsPerClient = 5;
        }).AddHubOptions<WebHub>(options => {
            options.MaximumReceiveMessageSize = int.MaxValue;
            options.MaximumParallelInvocationsPerClient = 5;
            options.EnableDetailedErrors = true;
        }).AddJsonProtocol(options => {
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        string spaDir = builder.Configuration.GetValue<string>("DiagServiceSettings:SpaDirectory")!;
        string spaPath = Expand(spaDir);
        services.AddSpaStaticFiles(conf => { conf.RootPath = spaPath; });

        var app = builder.Build();

        var settings = app.Services.GetService<IOptions<DiagServiceSettings>>().Value;

        app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseCors(x => x.SetIsOriginAllowed(x => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
        app.UseEndpoints(endpoints => {
            endpoints.MapHub<WebHub>("/web-hub");
            endpoints.MapHub<DiagnosticHub>("/diagnostics");
        });

        if (!settings.UseSpaProxy && !Directory.Exists(spaPath))
            throw new ApplicationException($"Diagnostics SPA directory not found: {spaPath}");

        app.UseSpa(spa => {
            spa.Options.DefaultPage = "/index.html";
            if (!settings.UseSpaProxy)
                app.UseSpaStaticFiles();

            if (settings.UseSpaProxy)
                spa.UseProxyToSpaDevelopmentServer(settings.SpaProxy);
        });

        if (!app.Urls.IsReadOnly)
        {
            app.Urls.Clear();

            foreach (string url in settings.Urls)
                app.Urls.Add(url);
        }

        app.Run();
    }


    static string? Expand(string? path) =>
        path == null
            ? null
            : Path.GetFullPath(Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
}