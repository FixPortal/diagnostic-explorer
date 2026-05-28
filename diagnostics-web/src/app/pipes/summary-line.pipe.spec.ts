import {SummaryLinePipe} from './summary-line.pipe';

describe('SummaryLinePipe', () => {
    const pipe = new SummaryLinePipe();

    it('keeps only the first line of a multi-line message', () => {
        expect(pipe.transform('Disk full\n  at Worker.run\n  at tick', 0)).toBe('Disk full');
    });

    it('handles carriage-return line endings', () => {
        expect(pipe.transform('First\r\nSecond', 0)).toBe('First');
    });

    it('truncates to maxLen when one is supplied', () => {
        expect(pipe.transform('Hello world', 5)).toBe('Hello');
    });

    it('does not truncate when maxLen is zero/falsy', () => {
        expect(pipe.transform('Hello world', 0)).toBe('Hello world');
    });

    it('passes empty/nullish values straight through', () => {
        expect(pipe.transform('', 10)).toBe('');
    });
});
