import { ChangeDetectionStrategy, Component, computed, inject, input, type WritableSignal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiChipSelectComponent, type FdUiChipSelectOption, FdUiEmojiPickerComponent, type FdUiEmojiPickerOption } from 'fd-ui-kit';
import type { FdUiEmojiPickerValue } from 'fd-ui-kit/emoji-picker/fd-ui-emoji-picker';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { FASTING_SYMPTOM_OPTIONS } from '../../../../shared/models/fasting.data';
import {
    FASTING_CHECK_IN_NOTES_MAX_LENGTH,
    FASTING_ENERGY_EMOJI_SCALE,
    FASTING_HUNGER_EMOJI_SCALE,
    FASTING_MOOD_EMOJI_SCALE,
    type FastingEmojiScaleOption,
} from '../../lib/fasting-page.constants';

@Component({
    selector: 'fd-fasting-check-in-card',
    imports: [TranslatePipe, FdUiChipSelectComponent, FdUiEmojiPickerComponent, FdUiTextareaComponent],
    templateUrl: './fasting-check-in-card.html',
    styleUrl: './fasting-check-in-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingCheckInCardComponent {
    private readonly translateService = inject(TranslateService);

    public readonly isSaving = input.required<boolean>();
    public readonly isEnding = input.required<boolean>();
    public readonly isUpdatingCycle = input.required<boolean>();
    public readonly hungerLevel = input.required<WritableSignal<number>>();
    public readonly energyLevel = input.required<WritableSignal<number>>();
    public readonly moodLevel = input.required<WritableSignal<number>>();
    public readonly selectedSymptoms = input.required<WritableSignal<string[]>>();
    public readonly notes = input.required<WritableSignal<string>>();

    protected readonly draftDisabled = (): boolean => this.isSaving() || this.isEnding() || this.isUpdatingCycle();
    protected readonly maxCheckInNotesLength = FASTING_CHECK_IN_NOTES_MAX_LENGTH;
    protected readonly hungerOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.HUNGER', FASTING_HUNGER_EMOJI_SCALE));
    protected readonly energyOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.ENERGY', FASTING_ENERGY_EMOJI_SCALE));
    protected readonly moodOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.MOOD', FASTING_MOOD_EMOJI_SCALE));
    private readonly translatedSymptomOptions = computed(() =>
        FASTING_SYMPTOM_OPTIONS.map<FdUiChipSelectOption>(symptom => {
            const label = this.translateService.instant(symptom.labelKey);
            const description = this.translateService.instant(symptom.descriptionKey);
            return {
                value: symptom.value,
                label,
                ariaLabel: `${label}. ${description}`,
                hint: description,
            };
        }),
    );
    protected readonly symptomOptions = computed(() => this.translatedSymptomOptions().filter(option => option.value !== 'good'));
    protected readonly noSymptomsOptions = computed(() => this.translatedSymptomOptions().filter(option => option.value === 'good'));

    protected setHungerLevel(value: FdUiEmojiPickerValue | null): void {
        this.setNumericLevel(this.hungerLevel(), value);
    }

    protected setEnergyLevel(value: FdUiEmojiPickerValue | null): void {
        this.setNumericLevel(this.energyLevel(), value);
    }

    protected setMoodLevel(value: FdUiEmojiPickerValue | null): void {
        this.setNumericLevel(this.moodLevel(), value);
    }

    protected setNotesValue(value: string | number | null): void {
        this.notes().set(value === null ? '' : String(value));
    }

    protected selectedHungerLabel(): string {
        return this.getSelectedScaleLabel('FASTING.CHECK_IN.HUNGER', this.hungerLevel()());
    }

    protected selectedEnergyLabel(): string {
        return this.getSelectedScaleLabel('FASTING.CHECK_IN.ENERGY', this.energyLevel()());
    }

    protected selectedMoodLabel(): string {
        return this.getSelectedScaleLabel('FASTING.CHECK_IN.MOOD', this.moodLevel()());
    }

    protected setWarningSymptoms(values: string[]): void {
        this.selectedSymptoms().set(values.filter(value => value !== 'good'));
    }

    protected setNoSymptoms(values: string[]): void {
        this.selectedSymptoms().set(values.includes('good') ? ['good'] : []);
    }

    private setNumericLevel(target: WritableSignal<number>, value: FdUiEmojiPickerValue | null): void {
        if (typeof value === 'number') {
            target.set(value);
        }
    }

    private buildEmojiPickerOptions(labelKey: string, scale: FastingEmojiScaleOption[]): Array<FdUiEmojiPickerOption<number>> {
        return scale.map(option => {
            const label = this.translateService.instant(`${labelKey}_LEVEL_${option.value}`);
            const description = this.translateService.instant(`${labelKey}_LEVEL_${option.value}_DESCRIPTION`);
            return {
                value: option.value,
                label,
                emoji: option.emoji,
                description,
                ariaLabel: `${label}. ${description}`,
                hint: `${label}. ${description}`,
            };
        });
    }

    private getSelectedScaleLabel(labelKey: string, value: number): string {
        return this.translateService.instant(`${labelKey}_LEVEL_${value}`);
    }
}
