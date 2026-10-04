import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { map } from 'rxjs';

import { LocalizedNumberPipe } from '../../../../../shared/i18n/localized-number.pipe';
import type { FastingStats } from '../../../../../shared/models/fasting.data';

@Component({
    selector: 'fd-fasting-stats-card',
    imports: [LocalizedNumberPipe, TranslatePipe, FdUiAccentSurfaceComponent, FdUiCardComponent],
    templateUrl: './fasting-stats-card.html',
    styleUrl: '../../fasting-page/fasting-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingStatsCardComponent {
    public readonly stats = input.required<FastingStats | null>();

    private readonly translate = inject(TranslateService);
    protected readonly language = toSignal(this.translate.onLangChange.pipe(map(event => event.lang)), {
        initialValue: this.translate.getCurrentLang() ?? 'en',
    });
    protected readonly streakDayUnitKey = computed(
        () => `FASTING.DAY_UNIT_${new Intl.PluralRules(this.language()).select(this.stats()?.currentStreak ?? 0).toUpperCase()}`,
    );

    protected readonly hasPersonalSummary = computed(() => {
        const stats = this.stats();

        return (
            stats !== null &&
            (stats.completionRateLast30Days > 0 ||
                stats.checkInRateLast30Days > 0 ||
                stats.lastCheckInAtUtc !== null ||
                stats.topSymptom !== null)
        );
    });
    protected readonly topSymptomLabelKey = computed(() => {
        const symptom = this.stats()?.topSymptom ?? null;
        return symptom === null ? 'FASTING.PERSONAL_SUMMARY.NO_SYMPTOM' : `FASTING.CHECK_IN.SYMPTOMS.${symptom.toUpperCase()}`;
    });
}
