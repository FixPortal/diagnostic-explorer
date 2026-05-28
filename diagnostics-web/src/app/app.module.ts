import {APP_INITIALIZER, NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';
import {MatLegacyTabsModule as MatTabsModule} from "@angular/material/legacy-tabs";
import {MatSidenavModule} from "@angular/material/sidenav";
import {BrowserAnimationsModule} from "@angular/platform-browser/animations";
import {MatToolbarModule} from "@angular/material/toolbar";
import {RetroNavComponent} from './retro-nav/retro-nav.component';
import {RetroDisplayComponent} from './retro-display/retro-display.component';
import {RealtimeNavComponent} from './realtime-nav/realtime-nav.component';
import {RealtimeDisplayComponent} from './realtime-display/realtime-display.component';
import {MatIconModule} from "@angular/material/icon";
import {MatLegacyButtonModule as MatButtonModule} from "@angular/material/legacy-button";
import {HttpClientModule} from '@angular/common/http';
import {MatLegacyTableModule as MatTableModule} from '@angular/material/legacy-table';
import {MatLegacyInputModule as MatInputModule} from '@angular/material/legacy-input';
import {MatLegacyListModule as MatListModule} from '@angular/material/legacy-list';
import {FormsModule} from '@angular/forms';
import {RealtimeCategoryComponent} from './realtime-category/realtime-category.component';
import {MatExpansionModule} from '@angular/material/expansion';
import {MatLegacyCardModule as MatCardModule} from '@angular/material/legacy-card';
import {RealtimeEventsComponent} from './realtime-events/realtime-events.component';
import {MatLegacyTooltipModule as MatTooltipModule} from '@angular/material/legacy-tooltip';
import {MatLegacySnackBarModule as MatSnackBarModule} from '@angular/material/legacy-snack-bar';
import {EventFilterComponent} from './event-filter/event-filter.component';
import {MatLegacyCheckboxModule as MatCheckboxModule} from '@angular/material/legacy-checkbox';
import {SetPropertyDialogComponent} from './set-property-dialog/set-property-dialog.component';
import {MatLegacyDialogModule as MatDialogModule} from '@angular/material/legacy-dialog';
import {InfoDialogComponent} from './info-dialog/info-dialog.component';
import {ExecOperationsComponent} from './exec-operations/exec-operations.component';
import {MatLegacyMenuModule as MatMenuModule} from '@angular/material/legacy-menu';
import {MatLegacySelectModule as MatSelectModule} from '@angular/material/legacy-select';
import {MatDatepickerModule} from '@angular/material/datepicker';
import {MAT_DATE_LOCALE, MatNativeDateModule} from '@angular/material/core';
import {APP_BASE_HREF, DatePipe} from '@angular/common';
import {SummaryLinePipe} from './pipes/summary-line.pipe';
import {MatLegacyProgressBarModule as MatProgressBarModule} from '@angular/material/legacy-progress-bar';
import {LevelNamePipe} from './pipes/level-name.pipe';
import {AngularSplitModule} from 'angular-split';
import {CollapsibleRegionComponent} from "./collapsible-region/collapsible-region.component";
import {getBaseLocation} from "./util/util";
import {BASE_API_URL} from "../injectionTokens";
import {environment} from "../environments/environment";

@NgModule({
    declarations: [
        AppComponent,
        RetroNavComponent,
        RetroDisplayComponent,
        RealtimeNavComponent,
        RealtimeDisplayComponent,
        RealtimeCategoryComponent,
        RealtimeEventsComponent,
        EventFilterComponent,
        SetPropertyDialogComponent,
        InfoDialogComponent,
        ExecOperationsComponent,
        SummaryLinePipe,
        LevelNamePipe,
        CollapsibleRegionComponent
    ],
    imports: [
        BrowserModule,
        AppRoutingModule,
        MatDialogModule,
        MatTabsModule,
        MatSidenavModule,
        BrowserAnimationsModule,
        MatToolbarModule,
        MatIconModule,
        MatButtonModule,
        MatSnackBarModule,
        HttpClientModule,
        MatTableModule,
        MatInputModule,
        FormsModule,
        MatExpansionModule,
        MatCardModule,
        MatTooltipModule,
        MatCheckboxModule,
        MatMenuModule,
        MatSelectModule,
        MatDatepickerModule,
        MatNativeDateModule,
        MatProgressBarModule,
        AngularSplitModule,
        MatListModule
    ],
    providers: [
        {provide: MAT_DATE_LOCALE, useValue: 'en-GB'},
        {provide: APP_BASE_HREF, useFactory: getBaseLocation},
        {provide: BASE_API_URL, useValue: environment.apiRoot},
        DatePipe
    ],
    bootstrap: [AppComponent]
})
export class AppModule {
}
