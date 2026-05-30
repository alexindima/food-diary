import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { FdUiLoaderComponent } from '../loader/fd-ui-loader';

@Component({
    selector: 'fd-ui-section-loading-state',
    imports: [FdUiLoaderComponent],
    templateUrl: './fd-ui-section-loading-state.html',
    styleUrl: './fd-ui-section-state.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSectionLoadingStateComponent {
    public readonly loadingLabel = input<string | null>('Loading');
}
