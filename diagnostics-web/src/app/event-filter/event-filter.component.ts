import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {FilterCriteria} from '../Model/FilterCriteria';
import {Watch} from '../util/Watch';

@Component({
    selector: 'app-event-filter',
    templateUrl: './event-filter.component.html',
    styleUrls: ['./event-filter.component.scss'],
    standalone: false
})
export class EventFilterComponent implements OnInit {


    watchEnabled = false;

    constructor() {
        this.watchEnabled = true;
    }

    ngOnInit(): void {
    }

    @Input()
    @Watch((_this: EventFilterComponent) => _this.filterVisibleChange.emit(_this.filterVisible))
    filterVisible = true;

    @Output()
    filterVisibleChange = new EventEmitter<boolean>();

    private _criteria = new FilterCriteria();

    @Input()
    @Watch((_this: EventFilterComponent) => _this.loadCriteria())
    get criteria(): FilterCriteria {
        return this._criteria;
    }

    set criteria(value: FilterCriteria) {
        this._criteria = value;
    }

    @Output()
    criteriaChange = new EventEmitter<FilterCriteria>();

    @Watch((_this: EventFilterComponent) => _this.onCriteriaChanged())
    searchText = '';

    @Watch((_this: EventFilterComponent) => _this.onCriteriaChanged())
    info = false;

    @Watch((_this: EventFilterComponent) => _this.onCriteriaChanged())
    notice = false;

    @Watch((_this: EventFilterComponent) => _this.onCriteriaChanged())
    warn = false;

    @Watch((_this: EventFilterComponent) => _this.onCriteriaChanged())
    error = false;


    private onCriteriaChanged(): void {
        const criteria = new FilterCriteria();
        criteria.searchText = this.searchText;
        criteria.info = this.info;
        criteria.notice = this.notice;
        criteria.warn = this.warn;
        criteria.error = this.error;
        this._criteria = criteria;
        this.criteriaChange.emit(criteria);
    }

    private loadCriteria(): void {
        this.searchText = this._criteria.searchText;
        this.info = this._criteria.info;
        this.notice = this._criteria.notice;
        this.warn = this._criteria.warn;
        this.error = this._criteria.error;
    }
}
