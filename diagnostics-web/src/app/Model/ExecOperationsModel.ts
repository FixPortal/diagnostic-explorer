import {SubCat} from './SubCat';
import {Subject} from 'rxjs';
import {getErrorMessage, strEqCI} from '../util/util';
import {OperationSet} from './DiagResponse';
import {RealtimeModel} from './RealtimeModel';
import {OperationModel} from './OperationModel';
import {ExecOperationRequest} from './ExecOperationRequest';
import {OperationResponse} from './SetPropertyRequest';
import {Null} from '../util/Null';
import {Clipboard} from '@angular/cdk/clipboard';
import {DiagHubService} from '../services/diag-hub.service';

export class ExecOperationsModel {
    finished = new Subject<void>();
    readonly operations: OperationModel[] = [];
    activeOperation?: OperationModel;
    results = '';
    executing = false;
    executeDate: Null<Date> = null;

    constructor(readonly realtimeModel: RealtimeModel,
                readonly subCat: SubCat) {

        const opSet: OperationSet | undefined = this.realtimeModel.operationSets.find(os => strEqCI(os.id, this.subCat.operationSet));

        console.log('Found operations', opSet);

        if (opSet)
            this.operations = opSet.operations.map(op => new OperationModel(op));
    }

    closeClick() {
        this.finished.next();
        this.finished.complete();
    }

    selectOperation(op: OperationModel) {
        this.activeOperation = op;
    }

    handleMouseOver(evt: MouseEvent, op: OperationModel) {
        if (evt.buttons === 1)
            this.selectOperation(op);
    }

    async execute(): Promise<void> {
        try {
            this.executing = true;
            this.results = '';
            this.executeDate = null;

            const request = new ExecOperationRequest();
            request.id = this.realtimeModel.activeProcess!.id;
            request.path = this.subCat.cat.name + '|' + this.subCat.name;
            request.operation = this.activeOperation!.signature;
            request.arguments = this.activeOperation!.parameters.map(p => p.value);

            const result: OperationResponse = await this.realtimeModel.hubService.executeOperation(request);

            if (result.isSuccess)
                this.results = result.result ?? 'Success';
            else
                this.results = result.errorMessage;
        } catch (err) {
            console.log(err);
            this.results = getErrorMessage(err);
        } finally {
            this.executing = false;
            this.executeDate = new Date();
        }
    }

    copyToClipboard() {
        new Clipboard(document).copy(this.results);

        this.realtimeModel.snackBar.open('Result copied to clipboard', '', {
            horizontalPosition: 'center',
            verticalPosition: 'top',
            politeness: 'assertive',
            panelClass: 'value-copied-snackbar',
            duration: 1_000,
        });
    }
}

