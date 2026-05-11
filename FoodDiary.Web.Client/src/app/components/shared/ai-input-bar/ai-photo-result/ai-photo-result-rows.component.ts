import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { AiResultRow } from './ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-result-rows',
    templateUrl: './ai-photo-result-rows.component.html',
    styleUrl: './ai-photo-result.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoResultRowsComponent {
    public readonly rows = input.required<AiResultRow[]>();
}
