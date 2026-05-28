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

    it('reflects every field of an assigned criteria into its inputs', () => {
        const criteria = new FilterCriteria();
        criteria.searchText = 'disk';
        criteria.info = true;
        criteria.notice = true;
        criteria.warn = true;
        criteria.error = false;

        component.criteria = criteria;

        // loadCriteria() suppresses the @Watch callbacks while copying, so the
        // level flags survive instead of being clobbered by a re-entrant
        // onCriteriaChanged() rebuild.
        expect(component.searchText).toBe('disk');
        expect(component.info).toBe(true);
        expect(component.notice).toBe(true);
        expect(component.warn).toBe(true);
        expect(component.error).toBe(false);
    });

    it('does not echo criteriaChange while reflecting an inbound binding', () => {
        const emitted: FilterCriteria[] = [];
        component.criteriaChange.subscribe(value => emitted.push(value));

        const criteria = new FilterCriteria();
        criteria.searchText = 'disk';
        criteria.warn = true;

        component.criteria = criteria;

        // An inbound criteria binding is a load, not a user edit: it must not
        // fire criteriaChange back at the parent (two-way-binding loop hazard).
        expect(emitted).toHaveLength(0);
    });
});
