using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticExplorer.Util;
using DiagWebService.Hubs;
using log4net;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;


namespace DiagnosticExplorer;

public class RegistrationHandler
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(RegistrationHandler));

    private string _url;
    private Registration _registration;
    private HubServerAdapter _hubAdapter;
    private HubConnection _connection;

    private CancellationTokenSource _stopToken;
    private Task _registrationLoop;
    private Task _loggingTask;
    private Subject<DiagnosticMsg> _logSubject = new();
    private Channel<IList<DiagnosticMsg>> _logChannel;

    private Action<HttpConnectionOptions> _configureHttp;


    public RegistrationHandler(string url, Registration registration)
    {
        _url = url;
        _registration = registration;
    }

    public void Start(Action<HttpConnectionOptions> configureHttp = null)
    {
        _configureHttp = configureHttp;
        _stopToken = new CancellationTokenSource();
        _logChannel = Channel.CreateBounded<IList<DiagnosticMsg>>(
            new BoundedChannelOptions(1_000_000) {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

        _logSubject
            .Buffer(TimeSpan.FromSeconds(2), 50)
            .Where(evts => evts.Count != 0)
            .Subscribe(evts => _logChannel?.Writer.TryWrite(evts));

        _registrationLoop = Task.Run(() => RunRegistrationProcess(_stopToken.Token));
        _loggingTask = Task.Run(() => RunLoggingProcess(_stopToken.Token));

        Debug.WriteLine($"Diagnostics RegistrationHandler for {_url} started");
    }

    private async Task RunLoggingProcess(CancellationToken cancel)
    {
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                IList<DiagnosticMsg> messages = await _logChannel.Reader.ReadAsync(cancel);
                try
                {
                    Stopwatch watch1 = Stopwatch.StartNew();
                    byte[] data = ProtobufUtil.Compress(messages, 1024);
                    watch1.Stop();

                    Stopwatch watch2 = Stopwatch.StartNew();
                    while (_hubAdapter == null)
                        await Task.Delay(TimeSpan.FromSeconds(1), cancel);

                    Debug.WriteLine($"RegistrationHandler sending {data.Length} bytes");
                    await _hubAdapter.LogEvents(data).ConfigureAwait(false);
                    watch2.Stop();
                    Debug.WriteLine($"RegistrationHandler sent {data.Length} bytes, zip/send took {watch1.ElapsedMilliseconds}ms/{watch2.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to log {messages.Count} messages: {ex.Message}");
                }
            }

            Debug.WriteLine($"RunLoggingProcess HAS NOW STOPPED");
        }
        catch (OperationCanceledException ex)
        {
            Debug.WriteLine($"RegistrationHandler.RunLoggingProcess cancelled");
        }
    }

    private async Task RunRegistrationProcess(CancellationToken cancelToken)
    {
        TimeSpan delay = TimeSpan.Zero;

        while (!cancelToken.IsCancellationRequested)
            try
            {
                if (delay != TimeSpan.Zero) await Task.Delay(delay, cancelToken);

                delay = TimeSpan.FromSeconds(5);

                cancelToken.ThrowIfCancellationRequested();

                await OpenHub();

                cancelToken.ThrowIfCancellationRequested();

                RegistrationResponse response = await _hubAdapter.Register(_registration);

                delay = response.RenewTimeSeconds <= 0
                    ? TimeSpan.FromSeconds(20)
                    : TimeSpan.FromSeconds(response.RenewTimeSeconds);
            }
            catch (Exception ex)
            {
                //Something went wrong, so kill the connection and try again
                await CloseConnection();

                if (cancelToken.IsCancellationRequested)
                    return;

                Debug.WriteLine($"RunRegistrationProcess exception {ex?.Message}");
                string errorMessage = $"DiagnosticHostingService.RegistrationHandler for {_url} encountered an exception";
                _log.Warn(errorMessage, ex);
            }
    }

    private async Task CloseConnection()
    {
        _hubAdapter = null;
        Debug.WriteLine($"CloseConnection _hubAdapter set to null");

        HubConnection connection = _connection;
        _connection = null;
        try
        {
            if (connection != null)
                await connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async Task OpenHub()
    {
        if (_hubAdapter == null)
        {
            Debug.WriteLine("Diagnostic RegistrationHandler constructing connection");
            _connection = new HubConnectionBuilder()
                .WithUrl(_url, _configureHttp ?? (options => {
                    options.UseDefaultCredentials = true;
                }))
                .Build();

            _connection.Closed += HandleClosed;

            Debug.WriteLine("Diagnostic RegistrationHandler starting connection");
            await _connection.StartAsync(_stopToken.Token);

            Debug.WriteLine("Diagnostic RegistrationHandler connection started");
            _hubAdapter = new HubServerAdapter(_connection);
        }
    }

    private async Task HandleClosed(Exception ex)
    {
        Debug.WriteLine($"RegistrationHandler.HandleClosed {ex?.Message}");
        HubConnection currentConnection = _connection;
        HubServerAdapter currentAdapter = _hubAdapter;

        _connection = null;
        _hubAdapter = null;

        try
        {
            currentAdapter?.Dispose();
        }
        catch (Exception ex2)
        {
            Trace.WriteLine("RegistrationHandler.HandleClosed HubServerAdapter.Dispose: " + ex2);
        }

        try
        {
            if (currentConnection != null)
                await currentConnection.DisposeAsync();
        }
        catch (Exception ex2)
        {
            Trace.WriteLine("RegistrationHandler.HandleClosed HubConnection.DisposeAsync: " + ex2);
        }
    }

    public async Task Stop()
    {
        try
        {
            Task loopTask = _registrationLoop;
            _stopToken?.Cancel();

            _logSubject?.OnCompleted();
            _logSubject = null;

            _logChannel?.Writer.Complete();
            _logChannel = null;

            _registrationLoop = null;
            _stopToken = null;

            if (loopTask != null)
                await loopTask.ConfigureAwait(false);

            await Deregister();

            if (_connection != null)
                await _connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }

    private async Task Deregister()
    {
        try
        {
            if (_hubAdapter != null)
            {
                _log.Info("DiagnosticHostingService Deregistered");
                await _hubAdapter.Deregister(_registration);
                Debug.WriteLine("Deregistered successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to deregister {ex}");
            _log.Error(ex);
        }
    }

    public void LogEvent(DiagnosticMsg evt)
    {
        _logSubject.OnNext(evt);
    }
}