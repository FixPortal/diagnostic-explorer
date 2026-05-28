import {DatePipe} from '@angular/common';
import {RealtimeModel} from './RealtimeModel';

function makeHub() {
    return {
        connectionReady: {subscribe: jest.fn()},
        connectionStarted: {subscribe: jest.fn()},
        connection: {invoke: jest.fn().mockResolvedValue(undefined)},
        setPropertyValue: jest.fn().mockResolvedValue({}),
        removeProcess: jest.fn().mockResolvedValue(undefined),
    };
}

function makeModel(hub = makeHub(), dialog = {open: jest.fn()}, snackBar = {open: jest.fn()}) {
    const model = new RealtimeModel(hub as any, new DatePipe('en-US'), dialog as any, snackBar as any);
    return {model, hub, dialog, snackBar};
}

describe('RealtimeModel', () => {
    it('filters processes by online state and search text', () => {
        const {model} = makeModel();

        model.displayProcesses([
            {id: '1', processName: 'OrderWorker', machineName: 'SRV01', userName: 'svc', state: 'Online'} as any,
            {id: '2', processName: 'AuditWorker', machineName: 'SRV02', userName: 'svc', state: 'Offline'} as any,
        ]);

        // onlineOnly defaults true, so the offline process is already excluded.
        expect(model.filteredProcesses.map(p => p.id)).toEqual(['1']);

        model.processSearch = 'order';

        expect(model.filteredProcesses.map(p => p.id)).toEqual(['1']);
    });

    it('removes a process from both the full and filtered lists', () => {
        const {model} = makeModel();
        model.displayProcesses([
            {id: '1', processName: 'OrderWorker', machineName: 'SRV01', userName: 'svc', state: 'Online'} as any,
            {id: '2', processName: 'PayWorker', machineName: 'SRV02', userName: 'svc', state: 'Online'} as any,
        ]);

        model.removeProcess('1');

        expect(model.allProcesses.map(p => p.id)).toEqual(['2']);
        expect(model.filteredProcesses.map(p => p.id)).toEqual(['2']);
    });

    it('selects an event and opens the trace scope', () => {
        const {model} = makeModel();
        const previous = {isSelected: true} as any;
        const next = {isSelected: false} as any;
        model.selectedEvent = previous;

        model.setCurrentEvent(next);

        expect(previous.isSelected).toBe(false);
        expect(next.isSelected).toBe(true);
        expect(model.selectedEvent).toBe(next);
        expect(model.traceScopeVisible).toBe(true);
    });

    it('subscribes to a process when it is selected', async () => {
        const {model, hub} = makeModel();

        await model.selectProcess({id: 'p-1'} as any);

        expect(model.activeProcess).toEqual({id: 'p-1'});
        expect(hub.connection.invoke).toHaveBeenCalledWith('Subscribe', 'p-1');
    });

    it('opens an error dialog when setPropertyValue returns an error message', async () => {
        const hub = makeHub();
        hub.setPropertyValue.mockResolvedValue({errorMessage: 'Denied'});
        const {model, dialog, snackBar} = makeModel(hub);
        model.activeProcess = {id: 'p-1'} as any;

        await model.setPropertyValue({getPropertyPath: () => 'Config.Timeout'} as any, '15');

        expect(dialog.open).toHaveBeenCalled();
        expect(snackBar.open).not.toHaveBeenCalled();
    });

    it('confirms with a snackbar when setPropertyValue succeeds', async () => {
        const hub = makeHub();
        hub.setPropertyValue.mockResolvedValue({});
        const {model, dialog, snackBar} = makeModel(hub);
        model.activeProcess = {id: 'p-1'} as any;

        await model.setPropertyValue({getPropertyPath: () => 'Config.Timeout'} as any, '15');

        expect(snackBar.open).toHaveBeenCalledWith('Property set!', '', expect.any(Object));
        expect(dialog.open).not.toHaveBeenCalled();
    });
});
