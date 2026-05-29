using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticExplorer;
using DiagnosticExplorer.Common;
using Diagnostics.Service.Common.Transport;
using log4net;
using log4net.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Operation = DiagnosticExplorer.Operation;

namespace Diagnostics.Service.Common.Hubs;

public class RetroManager : IHostedService
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(RetroManager));
    private IRetroLogger? _logger;
    private Channel<IList<DiagnosticMsg>>? _writeChannel;
    private Task? _loggingTask;
    private Subject<IList<DiagnosticMsg>>? _logSubject;
    private long _writeQueueSize = 0;
    private readonly ConcurrentDictionary<string, RetroSearchProcess> _searches = new();
    public EventSink RetroEvents { get; } = EventSinkRepo.Default.GetSink("Retro Events", "Retro");


    private readonly IHostApplicationLifetime _lifetime;

    public RetroManager(IHostApplicationLifetime lifetime, IOptions<DiagServiceSettings> config)
    {
        Options = config.Value;
        // Lifecycle is now driven by the host (registered via AddHostedService); no ctor
        // self-wiring. _lifetime is kept so StartAsync can tie the drain loop to ApplicationStopping.
        _lifetime = lifetime;
    }

  
    public Task StartAsync(CancellationToken cancel)
    {
        DiagnosticManager.Register(this, "Retro Manager", "Retro");

        _writeQueueSize = 0;

        _writeChannel = Channel.CreateBounded<IList<DiagnosticMsg>>(new BoundedChannelOptions(1_000_000)
        {
            SingleReader = true, 
            FullMode = BoundedChannelFullMode.DropWrite,
        });


        _logSubject = new Subject<IList<DiagnosticMsg>>();

        _logSubject.SelectMany(list => list)
            .Buffer(TimeSpan.FromSeconds(1), 50)
            .Where(evts => evts.Count != 0)
            .Subscribe(evts => {
                if (_writeChannel.Writer.TryWrite(evts))
                    Interlocked.Add(ref _writeQueueSize, evts.Count);
            });


        _logger = Options.CreateRetroLogger();

        // Tie the drain loop to ApplicationStopping (not the host's start-abort token) so it
        // keeps running for the app's lifetime and cancels cleanly on shutdown.
        _loggingTask = Task.Run(() => RunLoop(_lifetime.ApplicationStopping));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancel)
    {
        _writeChannel?.Writer.Complete();

        _logSubject = null;
        return Task.CompletedTask;
    }

    private async Task RunLoop(CancellationToken cancel)
    {
        try
        {
            await foreach (var messages in _writeChannel.Reader.ReadAllAsync(cancel))
                await TryLog(messages, cancel);
        }
        catch (OperationCanceledException) {}
    }


    public long WriteQueueSize => _writeQueueSize;
    public int ItemsInQueue => _writeChannel.Reader.CanCount ? _writeChannel.Reader.Count : -1;

    [ExtendedProperty]
    public DiagServiceSettings Options { get; set; }

    [RateProperty(ExposeTotal = false, ExposeRate = true)]
    public RateCounter EventsQueued { get; set; } = new(3);

    [RateProperty(ExposeTotal = false, ExposeRate = true)]
    public RateCounter EventsWritten { get; set; } = new(3);


    private async Task TryLog(IList<DiagnosticMsg> messages, CancellationToken cancel)
    {
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await _logger.WriteMessages(messages, cancel);
                Interlocked.Add(ref _writeQueueSize, -1 * messages.Count);
                EventsWritten.Register(messages.Count);
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.Error(ex);
                await Task.Delay(TimeSpan.FromSeconds(1), cancel);
            }
        }
    }

    public IAsyncEnumerable<RetroMsg[]> GetRetroLog(RetroQuery query, CancellationToken cancel)
    {
        return _logger.GetMessages(query, cancel);
    }


    public void LogEvents(IList<DiagnosticMsg> messages)
    {
        Subject<IList<DiagnosticMsg>>? logSubject = _logSubject;

        if (logSubject != null)
        {
            logSubject.OnNext(messages);
            Interlocked.Add(ref _writeQueueSize, messages.Count);
            EventsQueued.Register(messages.Count);
        }
    }

    public Task StartRetroSearch(RetroQuery query, string connectionId, IWebHubClient client)
    {
        if (_searches.TryRemove(connectionId, out RetroSearchProcess? existingSearch))
            existingSearch.Cancel();

        RetroEvents.Info($"Retro search starting for connection {connectionId}", 
            JsonSerializer.SerializeToElement(query).ToString());

        RetroSearchProcess search = new(this, connectionId, client, query);
        _searches.TryAdd(connectionId, search);
        search.Finished += HandleSearchFinished;
        search.Start();
        return Task.CompletedTask;
    }


    public Task<long> RetroDelete(string[] idList)
    {
        RetroEvents.Info($"Retro delete starting {idList.Length} messages");

        return _logger.Delete(idList);
    }

    private void HandleSearchFinished(object? sender, EventArgs e)
    {
        RetroSearchProcess search = (RetroSearchProcess) sender!;
        RetroEvents.Info($"Retro search complete for connection {search.ClientId} in {search.SearchTime.TotalSeconds:N2}s", 
            JsonSerializer.SerializeToElement(search.Query).ToString());
    }

    public Task CancelRetroSearch(int searchId, string connectionId)
    {
        if (_searches.TryGetValue(connectionId, out RetroSearchProcess? running))
        {
            if (running.Query.SearchId == searchId)
            {
                running.Cancel();
                _searches.TryRemove(connectionId, out _);
            }
        }

        return Task.CompletedTask;
    }

   
}