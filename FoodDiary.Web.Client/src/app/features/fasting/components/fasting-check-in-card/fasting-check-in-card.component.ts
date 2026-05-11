import { ChangeDetectionStrategy, Component, input, output, type WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiChipSelectComponent, type FdUiChipSelectOption, FdUiEmojiPickerComponent, type FdUiEmojiPickerOption } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import type { FdUiEmojiPickerValue } from 'fd-ui-kit/emoji-picker/fd-ui-emoji-picker.component';

import type { FastingCheckInViewModel } from '../../pages/fasting-page.component';
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
    public readonly isActive = input.required<boolean>();
    public readonly isSaving = input.required<boolean>();
    public readonly isEnding = input.required<boolean>();
    public readonly isUpdatingCycle = input.required<boolean>();
    public readonly isExpanded = input.required<boolean>();
    public readonly latestCheckIn = input.required<FastingCheckInViewModel | null>();
    public readonly ctaKey = input.required<string>();
    public readonly hungerOptions = input.required<FdUiEmojiPickerOption<number>[]>();
    public readonly energyOptions = input.required<FdUiEmojiPickerOption<number>[]>();
    public readonly moodOptions = input.required<FdUiEmojiPickerOption<number>[]>();
    public readonly symptomOptions = input.required<FdUiChipSelectOption[]>();
    public readonly hungerLevel = input.required<WritableSignal<number>>();
    public readonly energyLevel = input.required<WritableSignal<number>>();
    public readonly moodLevel = input.required<WritableSignal<number>>();
    public readonly selectedSymptoms = input.required<WritableSignal<string[]>>();
    public readonly notes = input.required<WritableSignal<string>>();

    public readonly formOpen = output<void>();
    public readonly formClose = output<void>();
    public readonly save = output<void>();

    protected readonly draftDisabled = (): boolean => this.isSaving() || this.isEnding() || this.isUpdatingCycle();

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
}
