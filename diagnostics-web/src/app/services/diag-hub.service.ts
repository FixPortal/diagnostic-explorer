import {Inject, Injectable} from '@angular/core';
import {HubConnection} from '@microsoft/signalr';
import * as signalR from '@microsoft/signalr';
import {ReplaySubject} from 'rxjs';
import {OperationResponse, SetPropertyRequest} from '../Model/SetPropertyRequest';
import {plainToInstance} from 'class-transformer';
import {ExecOperationRequest} from '../Model/ExecOperationRequest';
import {RetroQuery} from '../Model/RetroQuery';
import {BASE_API_URL} from "../../injectionTokens";

@Injectable({
    providedIn: 'root'
})
export class DiagHubService {

    public connection?: HubConnection;
    public connectionReady = new ReplaySubject<HubConnection>(1);
    public connectionStarted = new ReplaySubject<HubConnection>(1);


    constructor(
        @Inject(BASE_API_URL) private baseUrl: string) {
    }

    public async connect(): Promise<void> {
        while (!this.connection) {
            try {

                const connection = new signalR.HubConnectionBuilder()
                    .withUrl(this.baseUrl)
                    .build();

                connection.onreconnecting(err => console.log('Hub reconnecting', err))
                connection.onreconnected(connectionId => console.log('Hub reconnected', connectionId))
                connection.onclose(err => this.handleConnectionClosed(err));
                await connection.start();

                // Assign this.connection BEFORE emitting: subscribers (e.g. RealtimeModel's
                // connectionStarted handler) call this.connection.invoke('Subscribe', ...). If the
                // field were still undefined at emit time the re-subscribe would silently no-op,
                // so after a reconnect the client would stop receiving realtime diagnostics.
                this.connection = connection;
                this.connectionReady.next(connection);
                this.connectionStarted.next(connection);
            } catch (err) {
                console.log(err);
                await new Promise(resolve => setTimeout(resolve, 1000));
            }
        }
    }

    async setPropertyValue(request: SetPropertyRequest): Promise<OperationResponse> {
        // await the RPC: passing the un-awaited Promise to plainToInstance produced a default
        // OperationResponse (isSuccess:false, empty errorMessage), so callers saw "Property set!"
        // even when the hub returned an error.
        const response = await this.connection!.invoke<OperationResponse>(`SetProperty`, request);
        return plainToInstance(OperationResponse, response);
    }

    async executeOperation(request: ExecOperationRequest): Promise<OperationResponse> {
        const response = await this.connection!.invoke<OperationResponse>(`ExecuteOperation`, request);
        return plainToInstance(OperationResponse, response);
    }

    async removeProcess(id: string): Promise<void> {
        await this.connection!.invoke('RemoveProcess', id);
    }

    async startRetroSearch(query: RetroQuery): Promise<void> {
        await this.connection!.invoke('StartRetroSearch', query);
    }

    async cancelRetroSearch(searchId: number): Promise<void> {
        await this.connection!.invoke('CancelRetroSearch', searchId);
    }

    private async handleConnectionClosed(err: Error | undefined) {
        this.connection = undefined;
        await this.connect();
    }

    async deleteRecords(toDelete: string[]): Promise<number> {
        return await this.connection!.invoke<number>('RetroDelete', toDelete);
    }
}
