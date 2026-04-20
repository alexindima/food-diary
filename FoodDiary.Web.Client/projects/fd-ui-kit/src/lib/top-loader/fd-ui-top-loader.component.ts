import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'fd-ui-top-loader',
    standalone: true,
    template: `
        <div class="fd-ui-top-loader" [class.fd-ui-top-loader--visible]="visible()" aria-hidden="true">
            <span class="fd-ui-top-loader__bar"></span>
        </div>
    `,
    styleUrl: './fd-ui-top-loader.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTopLoaderComponent {
    public readonly visible = input(false);
}
