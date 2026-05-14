using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticExplorer.Log4Net;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;


namespace DiagnosticExplorer;

public class DiagnosticHostingService
#if NET5_0_OR_GREATER
    : IHostedService
#endif
{
    private static DiagnosticHostingService _instance;
    private DiagnosticOptions _options;

    private RegistrationHandler[] _registrationHandlers;

    private Action<HttpConnectionOptions> _configureHttp;

    private DiagnosticHostingService(DiagnosticOptions options, Action<HttpConnectionOptions> configureHttp = null)
    {
        _options = options;
        _configureHttp = configureHttp;
    }

#if NET5_0_OR_GREATER

    public DiagnosticHostingService(IOptions<DiagnosticOptions> options, Action<HttpConnectionOptions> configureHttp = null)
        : this(options.Value, configureHttp)
    {
        Debug.WriteLine($"DiagnosticHostingService constructed {_options.Enabled} Uri [{_options.Uri}");
    }


    public async Task StartAsync(CancellationToken cancel)
    {
        Debug.WriteLine($"DiagnosticHostingService starting {_options.Enabled} Uri [{_options.Uri}");
        if (_options.Enabled)
        {
            _instance = this;
            StartHosting();
        }
    }

    public Task StopAsync(CancellationToken cancel)
    {
        return StopHosting();
    }

#endif


    private void StartHosting()
    {
        try
        {
            DiagnosticRetroAppender.SetLoggingAction(LogEvent);
            SystemStatus.Register();

            Registration registration = new() {
                ProcessId = Process.GetCurrentProcess().Id,
                InstanceId = Guid.NewGuid().ToString("N"),
                UserDomain = Environment.UserDomainName,
                UserName = Environment.UserName,
                MachineName = Environment.MachineName,
                ProcessName = Process.GetCurrentProcess().ProcessName.Replace(".vshost", "")
            };

            _registrationHandlers = Regex.Split(_options.Uri, @"\s|;|,")
                .Select(hubUrl => hubUrl.Trim())
                .Where(hubUrl => !string.IsNullOrWhiteSpace(hubUrl))
                .Select(hubUrl => new RegistrationHandler(hubUrl, registration))
                .ToArray();

            foreach (RegistrationHandler handler in _registrationHandlers)
                handler.Start(_configureHttp);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public async Task StopHosting()
    {
        try
        {
            DiagnosticRetroAppender.SetLoggingAction(null);
            await Task.WhenAll(_registrationHandlers.Select(handler => handler.Stop()).ToArray());
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        _registrationHandlers = null;
    }


    public static void Start(string url, Action<HttpConnectionOptions> configureHttp = null)
    {
        if (_instance == null)
        {
            DiagnosticOptions options = new(url);
            _instance = new DiagnosticHostingService(options, configureHttp);
            _instance.StartHosting();

        }
    }

    public static async Task Stop()
    {
        if (_instance != null)
        {

            await _instance.StopHosting();
            _instance = null;
        }
    }


    public static void LogEvent(DiagnosticMsg evt)
    {
        DiagnosticHostingService instance = _instance;
        if (instance != null)
            // Debug.WriteLine($"Sending to {instance._registrationHandlers?.Length} registration handlers");
            foreach (RegistrationHandler handler in instance._registrationHandlers ?? Array.Empty<RegistrationHandler>())
                handler.LogEvent(evt);
    }
}