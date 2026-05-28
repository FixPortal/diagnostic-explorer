import {Component, Inject, OnInit} from '@angular/core';
import {ExecOperationsModel} from '../Model/ExecOperationsModel';
import {MAT_DIALOG_DATA} from '@angular/material/dialog';

@Component({
    selector: 'app-exec-operations',
    templateUrl: './exec-operations.component.html',
    styleUrls: ['./exec-operations.component.scss'],
    standalone: false
})
export class ExecOperationsComponent implements OnInit {

    constructor(@Inject(MAT_DIALOG_DATA) readonly model: ExecOperationsModel) {
    }

    ngOnInit(): void {
    }

}
