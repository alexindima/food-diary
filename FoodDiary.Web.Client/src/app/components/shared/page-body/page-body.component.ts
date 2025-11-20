import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-page-body',
    standalone: true,
    template: `<div class="fd-page-body"><ng-content /></div>`,
    styleUrls: ['./page-body.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageBodyComponent {}
