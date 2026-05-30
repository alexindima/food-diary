import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'fd-ui-top-loader',
    templateUrl: './fd-ui-top-loader.html',
    styleUrl: './fd-ui-top-loader.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTopLoaderComponent {
    public readonly visible = input(false);
}
