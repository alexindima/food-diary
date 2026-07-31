import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { AiResultRow } from '../ai-photo-result-lib/ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-result-rows',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './ai-photo-result-rows.html',
    styleUrl: '../ai-photo-result.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoResultRowsComponent {
    public readonly rows = input.required<AiResultRow[]>();
    public readonly activeAnnotationId = input<string | null>(null);
    public readonly rowSelected = output<string>();
}
