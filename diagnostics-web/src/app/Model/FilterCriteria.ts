import {Null} from '../util/Null';
import * as _ from 'lodash';
import {Watch} from '../util/Watch';
import {IFilterableEvent} from './IFilterableEvent';
import {Level} from './Level';

export class FilterCriteria {


    constructor() {
        this.watchEnabled = true;
    }

    watchEnabled = false;

    @Watch((_this: FilterCriteria) => _this.initFilterFunc())
    info = false

    @Watch((_this: FilterCriteria) => _this.initFilterFunc())
    notice = false;

    @Watch((_this: FilterCriteria) => _this.initFilterFunc())
    warn = false;

    @Watch((_this: FilterCriteria) => _this.initFilterFunc())
    error = false;

    @Watch((_this: FilterCriteria) => _this.initFilterFunc())
    searchText = '';

    _filterFunc: (evt: IFilterableEvent) => boolean = _ => true;

    get isBlank(): boolean {
        return !this.searchText
            && this.info === this.warn
            && this.info === this.notice
            && this.info === this.error;
    }

    filter(evt: IFilterableEvent): boolean {
        return this._filterFunc(evt);
    }

    private createFilterFunc(): ((evt: IFilterableEvent) => boolean) {

        if (this.isBlank)
            return _ => true;

        let info = this.info;
        let notice = this.notice;
        let warn = this.warn;
        let error = this.error;

        let matcher: Null<RegExp> = null;

        if (!info && !warn && !error && !notice)
            info = notice = warn = error = true;

        try {
            if (this.searchText?.trim())
                matcher = new RegExp(this.searchText, 'i');
        } catch (err) {
            matcher = new RegExp(_.escapeRegExp(this.searchText), 'i');
        }

        return evt => {
            if (!info && evt.level <= Level.INFO)
                return false;

            if (!notice && evt.level > Level.INFO && evt.level <= Level.NOTICE)
                return false;

            if (!warn && evt.level > Level.NOTICE && evt.level <= Level.WARN)
                return false;

            if (!error && evt.level >= Level.ERROR)
                return false;

            if (evt.user && matcher?.test(evt.user)) return true;
            if (evt.machine && matcher?.test(evt.machine)) return true;
            if (evt.process && matcher?.test(evt.process)) return true;
            if (matcher?.test(evt.message)) return true;

            return matcher?.test(evt.detail) ?? true;
        };
    }

    private initFilterFunc(): void {
        this._filterFunc = this.createFilterFunc();
    }
}
