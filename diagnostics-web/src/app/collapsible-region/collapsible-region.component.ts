import {Component, Input, OnInit} from '@angular/core';
import {ScopeNode} from "../Model/ScopeNode";

@Component({
    selector: 'app-collapsible-region',
    templateUrl: './collapsible-region.component.html',
    styleUrls: ['./collapsible-region.component.scss'],
    standalone: false
})
export class CollapsibleRegionComponent implements OnInit {

    @Input()
    region?: ScopeNode;

    constructor() {
    }

    ngOnInit(): void {
    }

    public getClass(): string {
        return 'collapsible-region-level' + this.region?.level;
    }


}




