using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticExplorer;
using DiagnosticExplorer.Common;

namespace DiagWebService.ClientHandlers;


public class DiagnosticSubscription
{
    private static int _instanceCounter = 0;
    private int _instance;
    public DiagProcess Process { get; set; }
    public IDiagnosticClient? DiagnosticClient { get; private set; }
    private readonly ConcurrentDictionary<string, WebClientHandler> _webClients = new();
    private Task? _requestLoop;
    private CancellationTokenSource? _requestLoopCancelSource;
    private DiagnosticResponse? _lastResponse;
    public string ProcessId => Process.Id;
    private IDisposable? _eventSetSubscription;
    private IDisposable? _eventStreamSubscription;
    private readonly EventSinkRepo _eventRepo = new();
    private readonly object _startStopLock = new();
    private bool _streamingStarted = false;

    public DiagnosticSubscription(DiagProcess process)
    {
        _instance = Interlocked.Increment(ref _instanceCounter);
        Process = process;
    }

    public void SetDiagnosticClient(IDiagnosticClient? diagClient)
    {
        if (DiagnosticClient != diagClient)
        {
            StopRequestLoop();
            StopDiagClientEvents();
            StopWebClientEvents();

            DiagnosticClient = diagClient;
            string isNull = diagClient == null ? "NULL" : "NOT NULL";
            //Debug.WriteLine($"@@@@@@@@@@ DiagnosticSubscription {Process.Id} client set to {isNull}");
            StartIfRequired();
        }
    }

    private void StopWebClientEvents()
    {
        _streamingStarted = false;
        foreach (WebClientHandler handler in _webClients.Values)
        {
            //Debug.WriteLine($"@@@@@@@@@@ StopWebClientEventStreaming handler {handler.ConnectionId} stop streaming events");
            handler.StopStreamingEvents();
        }
    }

    public async Task AddWebClient(WebClientHandler webClient)
    {
        //Debug.WriteLine($"@@@@@@@@@@ DiagnosticSubscription.AddWebClient {webClient.ConnectionId} now there are {_webClients.Count} _streamingStarted: {_streamingStarted}");

        if (_lastResponse != null)
            await TrySend(webClient, _lastResponse);

        if (DiagnosticClient == null)
            await webClient.SetEvents(ProcessId, _eventRepo.GetEvents());

        lock (_startStopLock)
        {
            bool added = _webClients.TryAdd(webClient.ConnectionId, webClient);

            if (added && _streamingStarted)
                webClient.StartStreamingEvents(Process.Id, _eventRepo);

            StartIfRequired();
        }
    }


    public void RemoveWebClient(WebClientHandler webClient)
    {
        if (_webClients.ContainsKey(webClient.ConnectionId))
        {
            //Debug.WriteLine($"@@@@@@@@@@ DiagnosticSubscription.RemoveWebClient {webClient.ConnectionId} currently {_webClients.Count} clients before removal");

            webClient.StopStreamingEvents();

            lock (_startStopLock)
            {
                _webClients.TryRemove(webClient.ConnectionId, out _);
            }

            StopIfRequired();
        }
    }

    private void StartIfRequired()
    {
        lock (_startStopLock)
        {
            if (_webClients.Any() && DiagnosticClient != null && _eventStreamSubscription == null)
                StartDiagClientEvents();

            if (_webClients.Any() && DiagnosticClient != null && _requestLoop == null)
                StartRequestLoop();

        }
    }

    private void StartRequestLoop()
    {
        _requestLoopCancelSource = new CancellationTokenSource();
        _requestLoop = RunLoop(DiagnosticClient!, _requestLoopCancelSource.Token);
    }

    private void StopRequestLoop()
    {
        _requestLoopCancelSource?.Cancel();
        _requestLoop = null;
    }
    
    private void StartDiagClientEvents()
    {
        _eventRepo.Clear();
        IDiagnosticClient diagnosticClient = DiagnosticClient!;
        //Debug.WriteLine($"@@@@@@@@@@ DiagnosticSubscription StartEventSubscriptions");
        IDisposable eventSetSubscription = diagnosticClient.EventsSet.Subscribe(HandleInitialEventsArrived);
        IDisposable eventStreamSubscription = diagnosticClient.EventsStreamed.Subscribe(evt => 
        {
            //Debug.WriteLine($"@@@@@@@@@@ DiagnosticSubscription {_instance} received single event {evt.Id} {evt.SinkName}");
            _eventRepo.LogEvents(evt);
        });
        _eventSetSubscription = eventSetSubscription;
        _eventStreamSubscription = eventStreamSubscription;
        RunDetached(() => diagnosticClient.SubscribeEvents(),
            ex => HandleSubscribeEventsFailure(diagnosticClient, eventSetSubscription, eventStreamSubscription, ex));
    }

    private void StopDiagClientEvents()
    {
        _eventSetSubscription?.Dispose();
        _eventStreamSubscription?.Dispose();

        _streamingStarted = false;

        _eventSetSubscription = null;
        _eventStreamSubscription = null;
        if (DiagnosticClient != null)
            RunDetached(() => DiagnosticClient.UnsubscribeEvents());
    }

  
    private void RunDetached(Func<Task> action, Action<Exception>? onError = null)
    {
        try
        {
            Task task = action();
            if (task.IsCompletedSuccessfully)
                return;

            _ = ObserveDetachedTask(task, onError);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }

    private async Task ObserveDetachedTask(Task task, Action<Exception>? onError)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }

    private void HandleSubscribeEventsFailure(
        IDiagnosticClient diagnosticClient,
        IDisposable eventSetSubscription,
        IDisposable eventStreamSubscription,
        Exception ex)
    {
        lock (_startStopLock)
        {
            if (!ReferenceEquals(DiagnosticClient, diagnosticClient))
                return;

            if (!ReferenceEquals(_eventSetSubscription, eventSetSubscription)
                || !ReferenceEquals(_eventStreamSubscription, eventStreamSubscription))
                return;

            eventSetSubscription.Dispose();
            eventStreamSubscription.Dispose();
            _eventSetSubscription = null;
            _eventStreamSubscription = null;
            _streamingStarted = false;
        }

        Trace.WriteLine($"DiagnosticSubscription {Process.Id} failed to subscribe events: {ex.Message}");
    }

    private void HandleInitialEventsArrived(SystemEvent[] events)
    {
        //Debug.WriteLine($"@@@@@@@@@@ DiagnosticSubscription {_instance} initial events arrived {events.Length}");

        lock (_startStopLock)
        {
            if (_streamingStarted)
                return;

            _eventRepo.LogEvents(events);
            _streamingStarted = true;

            foreach (WebClientHandler handler in _webClients.Values)
                handler.StartStreamingEvents(Process.Id, _eventRepo);
        }
    }

    private void StopIfRequired()
    {
        lock (_startStopLock)
        {
            if (_webClients.Count == 0 && _requestLoop != null)
                StopRequestLoop();

            if (_webClients.Count == 0 && _eventStreamSubscription != null)
                StopDiagClientEvents();
        }
    }


    private async Task RunLoop(IDiagnosticClient client, CancellationToken cancelToken)
    {
        //Debug.WriteLine($"@@@@@@@@@@ RunLoop {Process.Id} enter");
        try
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    if (client != null)
                    {
                        DiagnosticResponse diags = await client.GetDiagnostics(cancelToken);
                        _lastResponse = diags;
                        //Debug.WriteLine($"@@@@@@@@@@ RunLoop got diags {Process.Id} {diags}");
                        await Task.WhenAll(_webClients.Values.Select(client => TrySend(client, diags)));
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    await Task.WhenAll(_webClients.Values.Select(client => TrySendError(client, ex.Message)));
                    //Debug.WriteLine($"@@@@@@@@@@ RunLoop {Process.Id} exception {ex.Message}");
                }

                await Task.Delay(2000, cancelToken);
            }
        }
        catch (TaskCanceledException) {}
        catch (OperationCanceledException) {}

        //Debug.WriteLine($"@@@@@@@@@@ RunLoop {Process.Id} exit");
    }

    private async Task TrySend(WebClientHandler client, DiagnosticResponse diags)
    {
        try
        {
            await client.ShowDiagnostics(Process.Id, diags);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"@@@@@@@@@@ RunLoop {Process.Id} TrySend fail {ex.Message}");
        }
    }

    private async Task TrySendError(WebClientHandler client, string message)
    {
        try
        {
            await client.ShowDiagnosticsError(Process.Id, message);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"@@@@@@@@@@ RunLoop {Process.Id} TrySendError fail {ex.Message}");
        }
    }
}
