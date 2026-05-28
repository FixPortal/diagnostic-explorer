import {Component, OnInit, ViewChild} from '@angular/core';
import {AppModel} from '../Model/AppModel';
import {DiagProcess} from '../Model/DiagProcess';
import {RealtimeModel} from '../Model/RealtimeModel';
import {MatMenuTrigger} from '@angular/material/menu';

@Component({
    selector: 'app-realtime-nav',
    templateUrl: './realtime-nav.component.html',
    styleUrls: ['./realtime-nav.component.scss']
})
export class RealtimeNavComponent implements OnInit {

    columnNames = ['machineName', 'userName', 'processName'];
    @ViewChild(MatMenuTrigger)
    contextMenu?: MatMenuTrigger;

    constructor(readonly app: AppModel, readonly model: RealtimeModel) {

    }

    ngOnInit(): void {
    }

    getProcess(item: any): DiagProcess {
        return item as DiagProcess;
    }

    contextMenuPosition = {x: '0px', y: '0px'};

    onContextMenu(event: MouseEvent, item: DiagProcess) {
        event.preventDefault();
        this.contextMenuPosition.x = event.clientX + 'px';
        this.contextMenuPosition.y = event.clientY + 'px';
        this.contextMenu!.menuData = {'item': item};
        this.contextMenu!.menu?.focusFirstItem('mouse');
        this.contextMenu!.openMenu();
    }
}
