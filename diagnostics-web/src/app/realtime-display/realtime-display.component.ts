import {Component, OnInit} from '@angular/core';
import {AppModel} from '../Model/AppModel';
import {RealtimeModel} from '../Model/RealtimeModel';

@Component({
    selector: 'app-realtime-display',
    templateUrl: './realtime-display.component.html',
    styleUrls: ['./realtime-display.component.scss'],
    standalone: false
})
export class RealtimeDisplayComponent implements OnInit {

    constructor(readonly model: RealtimeModel) {
    }

    ngOnInit(): void {
    }

}
