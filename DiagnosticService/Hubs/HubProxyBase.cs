using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Diagnostics.Service.Common.Hubs;

public class HubProxyBase
{
    private TimeSpan _timeout = TimeSpan.FromSeconds(10);

    public HubProxyBase() : this(new AsyncResultBucket())
    {
    }

    public HubProxyBase(AsyncResultBucket responses)
    {
        Responses = responses;
    }

    protected AsyncResultBucket Responses { get; }


    protected Task SendRequest(CancellationToken cancel, Func<string, Task> send)
    {
        return SendRequest<object>(cancel, send);
    }

    protected async Task<T> SendRequest<T>(CancellationToken cancel, Func<string, Task> send)
    {
        string requestId = Guid.NewGuid().ToString("N");
        Task<T> task = Responses.GetResult<T>(requestId, _timeout, cancel);
        await send(requestId);
        // return await (not await + .Result): surfaces the original exception rather than an
        // AggregateException, and avoids the sync-over-async .Result footgun.
        return await task;
    }
}