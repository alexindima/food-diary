import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { map } from 'rxjs';

import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { BodyTargetKey } from '../../lib/goals.facade';

@Component({
    selector: 'fd-goals-side-cards',
    imports: [TranslatePipe, LocalizedNumberPipe],
    templateUrl: './goals-side-cards.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsSideCardsComponent {
    private readonly translateService = inject(TranslateService);
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly water = input.required<number>();
    public readonly bodyTargets = input.required<Record<BodyTargetKey, number>>();
    public readonly waterChange = output<number>();
    public readonly bodyTargetChange = output<{ key: BodyTargetKey; value: number }>();
    protected readonly language = toSignal(this.translateService.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translateService),
    });
    protected readonly displayBodyTargets = computed(() => ({
        weight: this.measurements.displayWeight(this.bodyTargets().weight),
        waist: this.measurements.displayLength(this.bodyTargets().waist),
    }));

    protected numberValue(event: Event): number | null {
        return event.target instanceof HTMLInputElement ? Number(event.target.value) : null;
    }

    protected updateWater(event: Event): void {
        const value = this.numberValue(event);
        if (value !== null) {
            this.waterChange.emit(value);
        }
    }

    protected updateBodyTarget(key: BodyTargetKey, event: Event): void {
        const value = this.numberValue(event);
        if (value !== null) {
            const canonicalValue = key === 'weight' ? this.measurements.canonicalWeight(value) : this.measurements.canonicalLength(value);
            this.bodyTargetChange.emit({ key, value: canonicalValue });
        }
    }
}
