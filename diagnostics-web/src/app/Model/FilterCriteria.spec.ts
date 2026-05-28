import {FilterCriteria} from './FilterCriteria';
import {IFilterableEvent} from './IFilterableEvent';
import {Level} from './Level';

function event(partial: Partial<IFilterableEvent>): IFilterableEvent {
    return {level: Level.INFO, machine: '', user: '', process: '', message: '', detail: '', ...partial};
}

describe('FilterCriteria', () => {
    it('matches everything while the criteria is blank', () => {
        const criteria = new FilterCriteria();

        expect(criteria.isBlank).toBe(true);
        expect(criteria.filter(event({message: 'anything'}))).toBe(true);
    });

    it('matches message text case-insensitively', () => {
        const criteria = new FilterCriteria();
        criteria.searchText = 'timeout';

        expect(criteria.filter(event({level: Level.ERROR, message: 'Socket TIMEOUT'}))).toBe(true);
        expect(criteria.filter(event({level: Level.ERROR, message: 'Connected'}))).toBe(false);
    });

    it('matches any of machine, user or process, not just the message', () => {
        const criteria = new FilterCriteria();
        criteria.searchText = 'srv01';

        expect(criteria.filter(event({machine: 'SRV01', message: 'idle'}))).toBe(true);
        expect(criteria.filter(event({user: 'srv01-svc', message: 'idle'}))).toBe(true);
        expect(criteria.filter(event({message: 'idle'}))).toBe(false);
    });

    it('falls back to an escaped regex when the search text is not a valid regex', () => {
        const criteria = new FilterCriteria();
        criteria.searchText = '[';

        expect(criteria.filter(event({message: 'a [literal] bracket'}))).toBe(true);
        expect(criteria.filter(event({message: 'no bracket here'}))).toBe(false);
    });

    it('filters by level when a single level flag is set', () => {
        const criteria = new FilterCriteria();
        criteria.warn = true;

        expect(criteria.isBlank).toBe(false);
        expect(criteria.filter(event({level: Level.WARN}))).toBe(true);
        expect(criteria.filter(event({level: Level.INFO}))).toBe(false);
        expect(criteria.filter(event({level: Level.ERROR}))).toBe(false);
    });
});
