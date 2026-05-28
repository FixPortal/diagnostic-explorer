import {Component, OnInit} from '@angular/core';
import {RetroModel} from '../Model/RetroModel';

@Component({
    selector: 'app-retro-display',
    templateUrl: './retro-display.component.html',
    styleUrls: ['./retro-display.component.scss'],
    standalone: false
})
export class RetroDisplayComponent implements OnInit {

    columnNames = ['date', 'level', 'machine', 'user', 'process', 'message'];


    constructor(readonly model: RetroModel) {
    }


    ngOnInit(): void {
    }

}
