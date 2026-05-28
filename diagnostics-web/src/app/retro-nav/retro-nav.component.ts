import {Component, OnInit} from '@angular/core';
import {RetroModel} from '../Model/RetroModel';

@Component({
    selector: 'app-retro-nav',
    templateUrl: './retro-nav.component.html',
    styleUrls: ['./retro-nav.component.scss'],
    standalone: false
})
export class RetroNavComponent implements OnInit {


    constructor(readonly model: RetroModel) {
    }

    times = Array.from(Array(24).keys());
    hours = [
        {hours: 1, text: "1 Hour"},
        {hours: 2, text: "2 Hours"},
        {hours: 4, text: "4 Hours"},
        {hours: 8, text: "8 Hours"},
        {hours: 12, text: "12 Hours"},
        {hours: 24, text: "1 Day"},
        {hours: 2 * 24, text: "2 Days"},
        {hours: 5 * 24, text: "5 Days"},
        {hours: 7 * 24, text: "7 Days"},
        {hours: 14 * 24, text: "14 Days"},
        {hours: 30 * 24, text: "30 Days"},
    ];

    ngOnInit(): void {
    }

}
