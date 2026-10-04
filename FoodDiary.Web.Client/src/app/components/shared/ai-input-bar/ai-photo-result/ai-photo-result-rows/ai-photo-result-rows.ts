import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { injectCurrentLanguage } from '../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../shared/i18n/localized-number.pipe';
import type { AiResultRow } from '../ai-photo-result-lib/ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-result-rows',
    imports: [LocalizedNumberPipe, TranslatePipe],
    templateUrl: './ai-photo-result-rows.html',
    styleUrl: '../ai-photo-result.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoResultRowsComponent {
    protected readonly activeLang = injectCurrentLanguage();

    public readonly rows = input.required<AiResultRow[]>();
    public readonly activeAnnotationId = input<string | null>(null);
    public readonly rowSelected = output<string>();
}
