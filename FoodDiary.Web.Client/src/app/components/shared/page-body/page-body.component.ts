import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-page-body',
    standalone: true,
    templateUrl: './page-body.component.html',
    styleUrls: ['./page-body.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageBodyComponent {}
