using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticExplorer;

public class EventSinkRepo
{

    private readonly List<EventSinkStream> _sinkStreams = new();
    private readonly ReaderWriterLockSlim _eventStreamLock = new(LockRecursionPolicy.NoRecursion);
    // Keyed by the (name, category) tuple, not a "{name}.{category}" string: the latter collided
    // distinct sinks, e.g. ("a.b","c") and ("a","b.c") both mapped to "a.b.c".
    private readonly ConcurrentDictionary<(string Name, string Category), EventSink> _sinks = new();

    public static EventSinkRepo Default { get; }= new();

    public EventSink GetSink(string name, string category)
    {
        return _sinks.GetOrAdd((name, category), key => new EventSink(this, key.Name, key.Category));
    }

    public void LogEvent(SystemEvent evt)
    {
        GetSink(evt.SinkName, evt.SinkCategory).LogEvent(evt);
    }

    public void LogEvents(SystemEvent[] evts)
    {
        foreach (SystemEvent evt in evts)
            LogEvent(evt);
    }

    public EventSinkStream CreateSinkStream(TimeSpan buffer, int bufferSize)
    {
        _eventStreamLock.EnterWriteLock();
        try
        {
            EventSinkStream stream = new(_sinks.Values.SelectMany(sink => sink.Events).ToArray(), buffer, bufferSize);
            _sinkStreams.Add(stream);
            stream.Disposed += HandleEventStreamDisposed;
            return stream;
        }
        finally
        {
            _eventStreamLock.ExitWriteLock();
        }
    }

    public SystemEvent[] GetEvents()
    {
        return _sinks.Values.SelectMany(sink => sink.Events).ToArray();
    }

    private void HandleEventStreamDisposed(object sender, EventArgs e)
    {
        EventSinkStream stream = (EventSinkStream)sender;
        UnregisterStream(stream);
    }

    private void UnregisterStream(EventSinkStream stream)
    {
        _eventStreamLock.EnterWriteLock();
        try
        {
            _sinkStreams.Remove(stream);
            stream.EventChannel.Writer.TryComplete();
        }
        finally
        {
            _eventStreamLock.ExitWriteLock();
        }
        stream.Disposed -= HandleEventStreamDisposed;
    }

    internal void RegisterEvent(SystemEvent evt)
    {
        _eventStreamLock.EnterReadLock();
        try
        {
            foreach (EventSinkStream stream in _sinkStreams)
                stream.StreamEvent(evt);
        }
        finally
        {
            _eventStreamLock.ExitReadLock();
        }
    }

    public void Clear()
    {
        _sinks.Clear();
    }
}