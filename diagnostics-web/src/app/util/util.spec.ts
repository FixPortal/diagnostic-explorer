import {getBaseLocation, getErrorMessage, strEqCI, today} from './util';

describe('util helpers', () => {
    describe('strEqCI', () => {
        it('treats strings as equal ignoring case', () => {
            expect(strEqCI('ProcA', 'proca')).toBe(true);
            expect(strEqCI('ProcA', 'ProcB')).toBe(false);
        });

        it('treats two null values as equal but a null and a value as unequal', () => {
            expect(strEqCI(null, null)).toBe(true);
            expect(strEqCI('ProcA', null)).toBe(false);
            expect(strEqCI(null, 'ProcA')).toBe(false);
        });
    });

    describe('getBaseLocation', () => {
        // getBaseLocation derives the app base href from the first path segment.
        // We drive the real location via history.pushState rather than stubbing
        // window.location, which modern Chrome refuses to let us redefine.
        const original = window.location.pathname;
        afterEach(() => history.replaceState({}, '', original));

        it('normalizes the current path into an application base href', () => {
            history.replaceState({}, '', '/diagnostics/app');

            expect(getBaseLocation()).toBe('/diagnostics/');
        });

        it('falls back to a root base href at the site root', () => {
            history.replaceState({}, '', '/');

            expect(getBaseLocation()).toBe('/');
        });
    });

    describe('getErrorMessage', () => {
        it('extracts the best available error message', () => {
            expect(getErrorMessage({error: {exceptionMessage: 'boom'}})).toBe('boom');
            expect(getErrorMessage({error: {message: 'inner'}})).toBe('inner');
            expect(getErrorMessage({message: 'fallback'})).toBe('fallback');
        });

        it('returns the string itself when handed a raw string', () => {
            expect(getErrorMessage('plain failure')).toBe('plain failure');
        });
    });

    describe('today', () => {
        it('returns today at midnight', () => {
            const value = today();

            expect(value.getHours()).toBe(0);
            expect(value.getMinutes()).toBe(0);
            expect(value.getSeconds()).toBe(0);
            expect(value.getMilliseconds()).toBe(0);
        });
    });
});
