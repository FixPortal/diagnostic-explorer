using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticExplorer;
using DiagnosticExplorer.Util;
using log4net;
using Microsoft.AspNetCore.SignalR.Client;

namespace DiagWebService.Hubs;

internal class HubServerAdapter : IDiagnosticHubClient
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(HubServerAdapter));
    private Task _writeEventTask;
    private CancellationTokenSource _writeEventCancel;

    private readonly HubConnection _hubConn;

    public HubServerAdapter(HubConnection hubConn)
    {
        _hubConn = hubConn;

        _hubConn.On<string>(nameof(IDiagnosticHubClient.GetDiagnostics),
            async (requestId) => await GetDiagnostics(requestId));

        _hubConn.On<string, string, string>(nameof(IDiagnosticHubClient.SetProperty),
            async (requestId, context, value) => await SetProperty(requestId, context, value));

        _hubConn.On<string, string, string, string[]>(nameof(IDiagnosticHubClient.ExecuteOperation),
            async (requestId, path, operation, args) => await ExecuteOperation(requestId, path, operation, args));

        _hubConn.On(nameof(IDiagnosticHubClient.SubscribeEvents),
            async () => await SubscribeEvents());

        _hubConn.On(nameof(IDiagnosticHubClient.UnsubscribeEvents),
            async () => await UnsubscribeEvents());
    }

    public Task SubscribeEvents()
    {
        _writeEventCancel = new CancellationTokenSource();
        _writeEventTask = Task.Run(() => SendEventStream(_writeEventCancel.Token), _writeEventCancel.Token);
        return Task.CompletedTask;
    }

    public Task UnsubscribeEvents()
    {
        _writeEventCancel?.Cancel();
        _writeEventCancel = null;
        _writeEventTask = null;
        return Task.CompletedTask;
    }

    private async Task SendEventStream(CancellationToken cancel)
    {
        using EventSinkStream stream = EventSinkRepo.Default.CreateSinkStream(TimeSpan.FromMilliseconds(50), 100);

        try
        {
            SystemEvent[] initial = stream.InitialEvents;
            await _hubConn.InvokeCoreAsync<string>(nameof(IDiagnosticHubServer.SetEvents), new object[] { initial }, cancel);

            while (await stream.EventChannel.Reader.WaitToReadAsync(cancel))
            {
                IList<SystemEvent> item = await stream.EventChannel.Reader.ReadAsync(cancel);
                await _hubConn.InvokeCoreAsync<string>(nameof(IDiagnosticHubServer.StreamEvents), new object[] { item }, cancel);
            }
        }
        catch (OperationCanceledException)
        {
            Trace.WriteLine("HubServerAdapter.SendEventStream cancelled");
        }
    }

    public void Dispose()
    {
        UnsubscribeEvents();
    }


    public Task GetDiagnostics(string requestId)
    {
        return Task.Run(async () => {
            RpcResult<byte[]> result = null;
            try
            {
                DiagnosticResponse response = DiagnosticManager.GetDiagnostics();
                byte[] compress = ProtobufUtil.Compress(response, 1024);

                result = RpcResult<byte[]>.Success(requestId, compress);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                result = RpcResult<byte[]>.Fail(requestId, ex);
            }

            await _hubConn.InvokeCoreAsync<string>(nameof(IDiagnosticHubServer.GetDiagnosticsReturn), new object[] { result });
        });
    }

    public Task SetProperty(string requestId, string path, string value)
    {
        return Task.Run(async () => {
            RpcResult<OperationResponse> result = null;

            try
            {
                OperationResponse response = DiagnosticManager.SetProperty(path, value);
                result = RpcResult<OperationResponse>.Success(requestId, response);
            }
            catch (Exception ex)
            {
                result = RpcResult<OperationResponse>.Fail(requestId, ex);
            }
            finally
            {
                await _hubConn.InvokeCoreAsync<string>(nameof(IDiagnosticHubServer.SetPropertyReturn), new object[] { result });
            }
        });
    }

    public Task ExecuteOperation(string requestId, string path, string operation, string[] args)
    {
        return Task.Run(async () => {
            RpcResult<OperationResponse> result = null;

            try
            {
                OperationResponse response = DiagnosticManager.ExecuteOperation(path, operation, args);
                result = RpcResult<OperationResponse>.Success(requestId, response);
            }
            catch (Exception ex)
            {
                result = RpcResult<OperationResponse>.Fail(requestId, ex);
            }
            finally
            {
                await _hubConn.InvokeCoreAsync<string>(nameof(IDiagnosticHubServer.ExecuteOperationReturn), new object[] { result });
            }
        });
    }

    public async Task<RegistrationResponse> Register(Registration registration)
    {
        RpcResult<RegistrationResponse> response = await _hubConn.InvokeCoreAsync<RpcResult<RegistrationResponse>>(nameof(IDiagnosticHubServer.Register), new object[] { registration });
        if (!response.IsSuccess)
            throw new ApplicationException(response.Message);

        return response.Response;
    }

    public async Task Deregister(Registration registration)
    {
        if (_hubConn != null)
        {
            RpcResult response = await _hubConn.InvokeCoreAsync<RpcResult>(nameof(IDiagnosticHubServer.Deregister), new object[] { registration });
            if (!response.IsSuccess)
                throw new ApplicationException(response.Message);
        }
    }

    public async Task LogEvents(byte[] eventData)
    {
        RpcResult response = await _hubConn.InvokeCoreAsync<RpcResult>(nameof(IDiagnosticHubServer.LogEvents), new object[] { eventData });

        if (!response.IsSuccess)
            throw new ApplicationException(response.Message);
    }
}
