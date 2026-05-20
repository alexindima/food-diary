import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-ui-loader',
    templateUrl: './fd-ui-loader.component.html',
    styleUrls: ['./fd-ui-loader.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLoaderComponent {}
