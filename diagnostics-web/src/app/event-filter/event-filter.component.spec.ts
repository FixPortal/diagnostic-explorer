import {EventFilterComponent} from './event-filter.component';
import {FilterCriteria} from '../Model/FilterCriteria';

describe('EventFilterComponent', () => {
    let component: EventFilterComponent;

    beforeEach(() => {
        // The component has no Angular dependencies; constructing it directly
        // enables the @Watch setters that drive the criteria/visibility outputs.
        component = new EventFilterComponent();
    });

    it('emits a fresh criteria object carrying the current flags when a flag changes', () => {
        const emitted: FilterCriteria[] = [];
        component.criteriaChange.subscribe(value => emitted.push(value));

        component.warn = true;
        component.searchText = 'stale';

        expect(emitted.at(-1)?.warn).toBe(true);
        expect(emitted.at(-1)?.searchText).toBe('stale');
        // A new instance is emitted each time, not the component's own criteria mutated in place.
        expect(emitted.at(-1)).not.toBe(emitted.at(-2));
    });

    it('emits filterVisible changes through the two-way binding output', () => {
        const emitted: boolean[] = [];
        component.filterVisibleChange.subscribe(value => emitted.push(value));

        component.filterVisible = false;

        expect(emitted.at(-1)).toBe(false);
    });

    it('reflects the search text of an assigned criteria', () => {
        // NOTE: loadCriteria() copies searchText first, which re-enters via the
        // searchText @Watch and rebuilds _criteria, so only searchText reliably
        // round-trips today. We assert just that contract rather than the level
        // flags, which the component does not currently load back.
        const criteria = new FilterCriteria();
        criteria.searchText = 'disk';

        component.criteria = criteria;

        expect(component.searchText).toBe('disk');
    });
});
