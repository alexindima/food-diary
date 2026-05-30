import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-ui-loader',
    templateUrl: './fd-ui-loader.html',
    styleUrls: ['./fd-ui-loader.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLoaderComponent {}
