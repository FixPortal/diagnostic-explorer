import {Component, Inject, OnInit} from '@angular/core';
import {MAT_LEGACY_DIALOG_DATA as MAT_DIALOG_DATA} from '@angular/material/legacy-dialog';
import {InfoDialogData} from '../Model/InfoDialogData';

@Component({
    selector: 'app-info-dialog',
    templateUrl: './info-dialog.component.html',
    styleUrls: ['./info-dialog.component.scss']
})
export class InfoDialogComponent implements OnInit {

    constructor(@Inject(MAT_DIALOG_DATA) public data: InfoDialogData) {
    }

    ngOnInit(): void {
    }

}
