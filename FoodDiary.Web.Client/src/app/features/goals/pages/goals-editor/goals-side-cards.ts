import { ChangeDetectionStrategy, Component, computed, inject, input, model, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { map } from 'rxjs';

import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { BodyTargetKey } from '../../lib/goals.facade';
import { BODY_TARGET_MAXIMUMS, isBodyTargetValid } from './goals-editor.models';

const BODY_TARGET_FRACTION_DIGITS = 2;
const DISPLAY_PRECISION_FACTOR = 100;

@Component({
    selector: 'fd-goals-side-cards',
    imports: [TranslatePipe, LocalizedNumberPipe, FdUiFormErrorComponent],
    templateUrl: './goals-side-cards.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsSideCardsComponent {
    private readonly translateService = inject(TranslateService);
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly water = model.required<number>();
    public readonly bodyTargets = input.required<Record<BodyTargetKey, number>>();

    public readonly bodyTargetChange = output<{ key: BodyTargetKey; value: number }>();
    protected readonly language = toSignal(this.translateService.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translateService),
    });
    protected readonly displayBodyTargets = computed(() => ({
        weight: this.measurements.displayWeight(this.bodyTargets().weight, BODY_TARGET_FRACTION_DIGITS),
        waist: this.measurements.displayLength(this.bodyTargets().waist, BODY_TARGET_FRACTION_DIGITS),
    }));

    protected readonly weightInvalid = computed(() => !isBodyTargetValid('weight', this.bodyTargets().weight));
    protected readonly waistInvalid = computed(() => !isBodyTargetValid('waist', this.bodyTargets().waist));
    protected readonly weightMaximum = computed(() => this.displayMaximum('weight'));
    protected readonly waistMaximum = computed(() => this.displayMaximum('waist'));

    private displayMaximum(key: BodyTargetKey): number {
        const limit = BODY_TARGET_MAXIMUMS[key];
        const displayed =
            key === 'weight'
                ? this.measurements.displayWeight(limit, BODY_TARGET_FRACTION_DIGITS)
                : this.measurements.displayLength(limit, BODY_TARGET_FRACTION_DIGITS);
        const canonical = key === 'weight' ? this.measurements.canonicalWeight(displayed) : this.measurements.canonicalLength(displayed);
        return canonical > limit
            ? Math.round((displayed - 1 / DISPLAY_PRECISION_FACTOR) * DISPLAY_PRECISION_FACTOR) / DISPLAY_PRECISION_FACTOR
            : displayed;
    }

    protected numberValue(event: Event): number | null {
        return event.target instanceof HTMLInputElement ? Number(event.target.value) : null;
    }

    protected updateWater(event: Event): void {
        const value = this.numberValue(event);
        if (value !== null) {
            this.water.set(value);
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
