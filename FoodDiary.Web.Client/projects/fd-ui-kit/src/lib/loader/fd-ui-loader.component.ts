import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'fd-ui-loader',
    standalone: true,
    template: `
        <div class="fd-ui-loader" role="status" aria-label="Loading">
            <span class="fd-ui-loader__spinner" aria-hidden="true"></span>
        </div>
    `,
    styleUrls: ['./fd-ui-loader.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiLoaderComponent {}
