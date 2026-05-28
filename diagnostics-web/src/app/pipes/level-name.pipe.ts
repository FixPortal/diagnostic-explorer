import {Pipe, PipeTransform} from '@angular/core';
import {Level} from '../Model/Level';

@Pipe({
    name: 'levelName',
    standalone: false
})
export class LevelNamePipe implements PipeTransform {

    transform(value: number): string {
        return Level.LevelToString(value);
    }

}
