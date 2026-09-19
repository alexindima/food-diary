import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { map } from 'rxjs';

import { resolveTranslateLanguage } from '../../../../../shared/i18n/translate-language.utils';
import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import type { TdeeInsight } from '../../../models/tdee-insight.data';

@Component({
    selector: 'fd-tdee-insight-card-details',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './tdee-insight-card-details.html',
    styleUrl: '../tdee-insight-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightCardDetailsComponent {
    private readonly translate = inject(TranslateService);
    protected readonly locale = toSignal(this.translate.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translate),
    });

    protected readonly measurements = inject(MeasurementSystemService);
    public readonly insight = input.required<TdeeInsight>();
    public readonly weightTrendFormatted = input.required<string | null>();
    protected readonly displayWeightTrend = computed(() => {
        const value = Number(this.weightTrendFormatted());
        return Number.isFinite(value) ? this.measurements.displayWeight(value) : null;
    });
}
