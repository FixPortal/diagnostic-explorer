import {Component} from '@angular/core';
import {AppModel} from './Model/AppModel';
import {RealtimeModel} from './Model/RealtimeModel';
import {RetroModel} from './Model/RetroModel';
import {ScopeNode} from "./Model/ScopeNode";

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss'],
    providers: [AppModel, RealtimeModel, RetroModel],
    standalone: false
})
export class AppComponent {
    title = 'diagnostics-web';
    region?: ScopeNode;

    constructor(readonly appModel: AppModel) {
        appModel.start();

        this.region = ScopeNode.parseTraceScope(this._sampleTrace);
        console.log(this.region);
    }


    _sampleTrace = `19541 [1] INFO  WidgetSample.Form1 (null) - 
  
[00.000] [00.000] BEGIN btnTraceScope_Click (2.584 seconds)
[00.000] [00.000] In Trace Scope Button Click 1 InvokeRequired: False
[00.104] [00.104] In the async bit B1
[00.104] [00.000] In the async bit A1
[00.106] [00.002] BEGIN TestTraceScope1 (1.358 seconds)
    [00.106] [00.000] ########## 0 ########## About to call TestTraceScope2() 3 times
    [00.137] [00.032] BEGIN TestTraceScope2 (0.724 seconds)
        [00.137] [00.000] BEGIN TestTraceScope2 (0.376 seconds)
            [00.137] [00.000] BEGIN TestTraceScope2 (0.188 seconds)
                [00.137] [00.000] ########## 0 ########## About to call TestTraceScope3() 1 times
                [00.167] [00.030] BEGIN TestTraceScope3 (0.127 seconds)
                    [00.167] [00.000] ########## 0 ########## About to call TestTraceScope4()
                    [00.199] [00.031] BEGIN TestTraceScope4 (0.063 seconds)
                        [00.214] [00.015] In the async bit B2
                        [00.230] [00.016] ########## 0 ########## Your lucky random number is 1168094842
                        [00.262] [00.032] ########## 0 ########## Here's a multiline trace message
                                          which, as you can see,
                                          has more than one line
                        [00.294] [00.032] ########## 0 ########## Just called TestTraceScope4()
                    [00.262] [-00.032] END TestTraceScope4 (0.063 seconds)
                    [00.326] [00.064] ########## 0 ########## Just called TestTraceScope3()
                [00.294] [-00.032] END TestTraceScope3 (0.127 seconds)
                [00.326] [00.032] ########## 0 ########## About to call TestTraceScope3() 1 times
                [00.356] [00.031] BEGIN TestTraceScope3 (0.126 seconds)
                    [00.356] [00.000] ########## 0 ########## About to call TestTraceScope4()
                    [00.388] [00.031] BEGIN TestTraceScope4 (0.063 seconds)
                        [00.420] [00.032] ########## 0 ########## Your lucky random number is 1413062265
                        [00.451] [00.032] ########## 0 ########## Here's a multiline trace message
                                          which, as you can see,
                                          has more than one line
                        [00.482] [00.031] ########## 0 ########## Just called TestTraceScope4()
                    [00.451] [-00.031] END TestTraceScope4 (0.063 seconds)
                    [00.514] [00.063] ########## 0 ########## Just called TestTraceScope3()
                [00.482] [-00.032] END TestTraceScope3 (0.126 seconds)
            [00.326] [-00.156] END TestTraceScope2 (0.188 seconds)
            [00.514] [00.188] ########## 0 ########## About to call TestTraceScope3() 2 times
            [00.545] [00.031] BEGIN TestTraceScope3 (0.128 seconds)
                [00.545] [00.000] ########## 0 ########## About to call TestTraceScope4()
                [00.577] [00.032] BEGIN TestTraceScope4 (0.064 seconds)
                    [00.609] [00.032] ########## 0 ########## Your lucky random number is 1028674384
                    [00.641] [00.032] ########## 0 ########## Here's a multiline trace message
                                      which, as you can see,
                                      has more than one line
                    [00.673] [00.032] ########## 0 ########## Just called TestTraceScope4()
                [00.641] [-00.032] END TestTraceScope4 (0.064 seconds)
                [00.705] [00.064] BEGIN TestTraceScope3 (0.125 seconds)
                    [00.705] [00.000] ########## 0 ########## About to call TestTraceScope4()
                    [00.735] [00.030] BEGIN TestTraceScope4 (0.063 seconds)
                        [00.766] [00.032] ########## 0 ########## Your lucky random number is 474424832
                        [00.798] [00.031] ########## 0 ########## Here's a multiline trace message
                                          which, as you can see,
                                          has more than one line
                        [00.830] [00.032] ########## 0 ########## Just called TestTraceScope4()
                    [00.798] [-00.032] END TestTraceScope4 (0.063 seconds)
                    [00.862] [00.064] ########## 0 ########## Just called TestTraceScope3()
                [00.830] [-00.032] END TestTraceScope3 (0.125 seconds)
            [00.673] [-00.157] END TestTraceScope3 (0.128 seconds)
        [00.514] [-00.159] END TestTraceScope2 (0.376 seconds)
        [00.894] [00.380] BEGIN TestTraceScope2 (0.346 seconds)
            [00.894] [00.000] ########## 0 ########## About to call TestTraceScope3() 2 times
            [00.925] [00.032] BEGIN TestTraceScope3 (0.125 seconds)
                [00.925] [00.000] ########## 0 ########## About to call TestTraceScope4()
                [00.956] [00.031] BEGIN TestTraceScope4 (0.063 seconds)
                    [00.987] [00.031] ########## 0 ########## Your lucky random number is 1590340735
                    [01.019] [00.032] ########## 0 ########## Here's a multiline trace message
                                      which, as you can see,
                                      has more than one line
                    [01.051] [00.032] ########## 0 ########## Just called TestTraceScope4()
                [01.019] [-00.032] END TestTraceScope4 (0.063 seconds)
                [01.083] [00.064] BEGIN TestTraceScope3 (0.126 seconds)
                    [01.083] [00.000] ########## 0 ########## About to call TestTraceScope4()
                    [01.114] [00.031] BEGIN TestTraceScope4 (0.063 seconds)
                        [01.146] [00.032] ########## 0 ########## Your lucky random number is 312804871
                        [01.177] [00.031] ########## 0 ########## Here's a multiline trace message
                                          which, as you can see,
                                          has more than one line
                        [01.208] [00.031] ########## 0 ########## Just called TestTraceScope4()
                    [01.177] [-00.031] END TestTraceScope4 (0.063 seconds)
                    [01.240] [00.063] ########## 0 ########## Just called TestTraceScope3()
                [01.208] [-00.032] END TestTraceScope3 (0.126 seconds)
            [01.051] [-00.158] END TestTraceScope3 (0.125 seconds)
            [01.271] [00.221] BEGIN TestTraceScope2 (0.192 seconds)
                [01.271] [00.000] ########## 0 ########## About to call TestTraceScope3() 1 times
                [01.304] [00.032] BEGIN TestTraceScope3 (0.128 seconds)
                    [01.304] [00.000] ########## 0 ########## About to call TestTraceScope4()
                    [01.336] [00.032] BEGIN TestTraceScope4 (0.064 seconds)
                        [01.368] [00.032] ########## 0 ########## Your lucky random number is 779552368
                        [01.400] [00.032] ########## 0 ########## Here's a multiline trace message
                                          which, as you can see,
                                          has more than one line
                        [01.432] [00.032] ########## 0 ########## Just called TestTraceScope4()
                    [01.400] [-00.032] END TestTraceScope4 (0.064 seconds)
                    [01.464] [00.064] ########## 0 ########## Just called TestTraceScope3()
                [01.432] [-00.032] END TestTraceScope3 (0.128 seconds)
                [01.464] [00.032] ########## 0 ########## Just called TestTraceScope2()
            [01.464] [-00.000] END TestTraceScope2 (0.192 seconds)
        [01.240] [-00.224] END TestTraceScope2 (0.346 seconds)
    [00.862] [-00.378] END TestTraceScope2 (0.724 seconds)
    [01.574] [00.712] In the async bit A2
[01.464] [-00.110] END TestTraceScope1 (1.358 seconds)
[02.584] [01.120] END btnTraceScope_Click (2.584 seconds)
`


}
