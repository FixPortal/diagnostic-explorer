import {LevelNamePipe} from './level-name.pipe';
import {Level} from '../Model/Level';

describe('LevelNamePipe', () => {
    const pipe = new LevelNamePipe();

    it('maps known numeric levels to their display names', () => {
        expect(pipe.transform(Level.ERROR)).toBe('Error');
        expect(pipe.transform(Level.WARN)).toBe('Warn');
        expect(pipe.transform(Level.INFO)).toBe('Info');
    });

    it('rounds a value down to the nearest band', () => {
        expect(pipe.transform(Level.ERROR + 1)).toBe('Error');
        expect(pipe.transform(Level.WARN - 1)).toBe('Notice');
    });

    it('returns Unknown for values below the lowest level', () => {
        expect(pipe.transform(0)).toBe('Unknown');
    });
});
