import {Component, Inject, OnInit} from '@angular/core';
import {PropModel} from '../Model/PropModel';
import {MAT_DIALOG_DATA, MatDialogRef} from '@angular/material/dialog';
import {PromptData, PromptResult} from '../util/PromptResult';

@Component({
    selector: 'app-set-property-dialog',
    templateUrl: './set-property-dialog.component.html',
    styleUrls: ['./set-property-dialog.component.scss'],
    standalone: false
})
export class SetPropertyDialogComponent implements OnInit {

    text: string = '';
    value: string = '';

    constructor(public dialogRef: MatDialogRef<SetPropertyDialogComponent>,
                @Inject(MAT_DIALOG_DATA) public prompt: PromptData) {

        this.text = prompt.text;
        this.value = prompt.value;
    }

    ngOnInit(): void {
    }


    onCancelClick(): void {
        this.dialogRef.close(new PromptResult('Cancel', ''));
    }

    onOkClick(): void {
        this.dialogRef.close(new PromptResult('OK', this.value));
    }

    handleKeyUp(evt: KeyboardEvent) {
        console.log(evt.key);
        if (evt.key === 'Enter')
            this.onOkClick();

        if (evt.key === 'Escape')
            this.onCancelClick();
    }
}
