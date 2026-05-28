import {Pipe, PipeTransform} from '@angular/core';

@Pipe({
    name: 'summaryLine',
    standalone: false
})
export class SummaryLinePipe implements PipeTransform {

    transform(value: string, maxLen: number): string {

        if (!value)
            return value;


        let s = value.replace(/\r?\n.*/g, '');
        if (maxLen)
            s = s.substring(0, maxLen);

        return s;
    }

}
