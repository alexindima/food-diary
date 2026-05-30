import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-page-body',
    templateUrl: './page-body.html',
    styleUrls: ['./page-body.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageBodyComponent {}
