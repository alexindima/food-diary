import {
    afterRenderEffect,
    ChangeDetectionStrategy,
    Component,
    computed,
    type ElementRef,
    inject,
    input,
    output,
    viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { map } from 'rxjs';

import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { BodyTargetKey } from '../../lib/goals.facade';
import { BODY_TARGET_MAXIMUMS, isBodyTargetValid, isGoalNumberValid } from './goals-editor.models';
import { GoalsNumberInputDirective } from './goals-number-input.directive';

const BODY_TARGET_FRACTION_DIGITS = 2;
const DISPLAY_PRECISION_FACTOR = 100;

@Component({
    selector: 'fd-goals-side-cards',
    imports: [TranslatePipe, LocalizedNumberPipe, FdUiFormErrorComponent, GoalsNumberInputDirective],
    templateUrl: './goals-side-cards.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsSideCardsComponent {
    private readonly translateService = inject(TranslateService);
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly water = input.required<number>();
    public readonly waterIncomplete = input.required<boolean>();
    public readonly bodyTargets = input.required<Record<BodyTargetKey, number>>();
    public readonly invalidBodyTargets = input.required<Record<BodyTargetKey, boolean>>();

    public readonly waterChange = output<number | null>();
    public readonly bodyTargetChange = output<{ key: BodyTargetKey; value: number | null }>();
    private readonly weightField = viewChild.required<ElementRef<HTMLInputElement>>('weightField');
    private readonly waistField = viewChild.required<ElementRef<HTMLInputElement>>('waistField');
    protected readonly language = toSignal(this.translateService.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translateService),
    });
    protected readonly displayBodyTargets = computed(() => ({
        weight: this.measurements.displayWeight(this.bodyTargets().weight, BODY_TARGET_FRACTION_DIGITS),
        waist: this.measurements.displayLength(this.bodyTargets().waist, BODY_TARGET_FRACTION_DIGITS),
    }));

    protected readonly weightInvalid = computed(
        () => this.invalidBodyTargets().weight || !isBodyTargetValid('weight', this.bodyTargets().weight),
    );
    protected readonly waterInvalid = computed(() => this.waterIncomplete() || !isGoalNumberValid(this.water()));
    protected readonly waistInvalid = computed(
        () => this.invalidBodyTargets().waist || !isBodyTargetValid('waist', this.bodyTargets().waist),
    );
    protected readonly weightMaximum = computed(() => this.displayMaximum('weight'));
    protected readonly waistMaximum = computed(() => this.displayMaximum('waist'));

    public constructor() {
        afterRenderEffect({
            write: () => {
                this.syncBodyTarget('weight', this.weightField().nativeElement);
                this.syncBodyTarget('waist', this.waistField().nativeElement);
            },
        });
    }

    private syncBodyTarget(key: BodyTargetKey, field: HTMLInputElement): void {
        // Writing a value would replace the browser's in-progress invalid edit.
        if (!this.invalidBodyTargets()[key]) {
            const value = String(this.displayBodyTargets()[key]);
            if (field.value !== value) {
                field.value = value;
            }
        }
    }

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

    protected updateBodyTarget(key: BodyTargetKey, field: HTMLInputElement): void {
        if (!field.validity.valid) {
            this.bodyTargetChange.emit({ key, value: null });
            return;
        }
        const value = field.value === '' ? 0 : field.valueAsNumber;
        const canonicalValue = key === 'weight' ? this.measurements.canonicalWeight(value) : this.measurements.canonicalLength(value);
        this.bodyTargetChange.emit({ key, value: canonicalValue });
    }
}
