import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-ui-loader',
    standalone: true,
    templateUrl: './fd-ui-loader.component.html',
    styleUrls: ['./fd-ui-loader.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLoaderComponent {}
