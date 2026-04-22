import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'fd-ui-top-loader',
    standalone: true,
    templateUrl: './fd-ui-top-loader.component.html',
    styleUrl: './fd-ui-top-loader.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTopLoaderComponent {
    public readonly visible = input(false);
}
