import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    inject,
    input,
    output,
    signal,
    type WritableSignal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiChipSelectComponent, type FdUiChipSelectOption, FdUiEmojiPickerComponent, type FdUiEmojiPickerOption } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import type { FdUiEmojiPickerValue } from 'fd-ui-kit/emoji-picker/fd-ui-emoji-picker.component';
import { EMPTY, type Observable } from 'rxjs';

import { LocalizationService } from '../../../../services/localization.service';
import {
    FASTING_ENERGY_EMOJI_SCALE,
    FASTING_HUNGER_EMOJI_SCALE,
    FASTING_MOOD_EMOJI_SCALE,
    type FastingEmojiScaleOption,
} from '../../lib/fasting-page.constants';
import { FASTING_SYMPTOM_OPTIONS } from '../../models/fasting.data';
import type { FastingCheckInViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingCheckInSummaryComponent } from '../fasting-check-in-summary/fasting-check-in-summary.component';

@Component({
    selector: 'fd-fasting-check-in-card',
    imports: [
        FormsModule,
        TranslatePipe,
        FdUiChipSelectComponent,
        FdUiEmojiPickerComponent,
        FdUiButtonComponent,
        FdUiCardComponent,
        FastingCheckInSummaryComponent,
    ],
    templateUrl: './fasting-check-in-card.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingCheckInCardComponent {
    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly currentLanguage = signal(this.localizationService.getCurrentLanguage());

    public readonly isActive = input.required<boolean>();
    public readonly isSaving = input.required<boolean>();
    public readonly isEnding = input.required<boolean>();
    public readonly isUpdatingCycle = input.required<boolean>();
    public readonly isExpanded = input.required<boolean>();
    public readonly latestCheckIn = input.required<FastingCheckInViewModel | null>();
    public readonly hungerLevel = input.required<WritableSignal<number>>();
    public readonly energyLevel = input.required<WritableSignal<number>>();
    public readonly moodLevel = input.required<WritableSignal<number>>();
    public readonly selectedSymptoms = input.required<WritableSignal<string[]>>();
    public readonly notes = input.required<WritableSignal<string>>();

    public readonly formOpen = output();
    public readonly formClose = output();
    public readonly save = output();

    protected readonly draftDisabled = (): boolean => this.isSaving() || this.isEnding() || this.isUpdatingCycle();
    protected readonly ctaKey = computed(() =>
        this.latestCheckIn() === null ? 'FASTING.CHECK_IN.ADD_ACTION' : 'FASTING.CHECK_IN.UPDATE_ACTION',
    );
    protected readonly hungerOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.HUNGER', FASTING_HUNGER_EMOJI_SCALE));
    protected readonly energyOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.ENERGY', FASTING_ENERGY_EMOJI_SCALE));
    protected readonly moodOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.MOOD', FASTING_MOOD_EMOJI_SCALE));
    protected readonly symptomOptions = computed(() => {
        this.currentLanguage();

        return FASTING_SYMPTOM_OPTIONS.map<FdUiChipSelectOption>(symptom => {
            const label = this.translateService.instant(symptom.labelKey);
            return {
                value: symptom.value,
                label,
                ariaLabel: label,
                hint: label,
            };
        });
    });

    public constructor() {
        ((this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.currentLanguage.set(this.localizationService.getCurrentLanguage());
            });
    }

    protected setHungerLevel(value: FdUiEmojiPickerValue | null): void {
        this.setNumericLevel(this.hungerLevel(), value);
    }

    protected setEnergyLevel(value: FdUiEmojiPickerValue | null): void {
        this.setNumericLevel(this.energyLevel(), value);
    }

    protected setMoodLevel(value: FdUiEmojiPickerValue | null): void {
        this.setNumericLevel(this.moodLevel(), value);
    }

    private setNumericLevel(target: WritableSignal<number>, value: FdUiEmojiPickerValue | null): void {
        if (typeof value === 'number') {
            target.set(value);
        }
    }

    private buildEmojiPickerOptions(labelKey: string, scale: FastingEmojiScaleOption[]): Array<FdUiEmojiPickerOption<number>> {
        this.currentLanguage();

        return scale.map(option => {
            const label = this.translateService.instant(`${labelKey}_LEVEL_${option.value}`);
            return {
                value: option.value,
                label,
                emoji: option.emoji,
                ariaLabel: label,
            };
        });
    }
}
