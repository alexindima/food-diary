import { CdkTrapFocus } from '@angular/cdk/a11y';
import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { type FieldTree, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { CYCLE_SYMPTOM_FIELDS, type CycleSymptomField } from '../../lib/cycle-tracking.config';
import type { CycleDayFormModel } from '../../lib/cycle-tracking.facade';
import {
    BLEEDING_TYPE_BLEEDING,
    BLEEDING_TYPE_SPOTTING,
    type BleedingType,
    CYCLE_FLOW_HEAVY,
    CYCLE_FLOW_LIGHT,
    CYCLE_FLOW_MEDIUM,
    type CycleFlowLevel,
    type OvulationTestResult,
} from '../../models/cycle.data';

const SYMPTOM_SEVERITY_MILD = 3;
const SYMPTOM_SEVERITY_MODERATE = 6;
const SYMPTOM_SEVERITY_SEVERE = 9;
const SYMPTOM_SEVERITY_MILD_VALUE = String(SYMPTOM_SEVERITY_MILD);
const SYMPTOM_SEVERITY_MODERATE_VALUE = String(SYMPTOM_SEVERITY_MODERATE);
const SYMPTOM_SEVERITY_SEVERE_VALUE = String(SYMPTOM_SEVERITY_SEVERE);

@Component({
    selector: 'fd-cycle-day-editor-drawer',
    imports: [
        CdkTrapFocus,
        TranslatePipe,
        FormField,
        FormRoot,
        FdUiButtonComponent,
        FdUiDateInputComponent,
        FdUiInputComponent,
        FdUiSegmentedToggleComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
    ],
    templateUrl: './cycle-day-editor-drawer.html',
    styleUrl: './cycle-day-editor-drawer.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDayEditorDrawerComponent {
    public readonly dayForm = input.required<FieldTree<CycleDayFormModel>>();
    public readonly isSaving = input.required<boolean>();
    public readonly editingDate = input<string | null>(null);
    public readonly ovulationTestOptions = input.required<Array<FdUiSelectOption<OvulationTestResult>>>();
    public readonly fertilityEnabled = input(false);
    public readonly closed = output();

    protected readonly symptomFields = CYCLE_SYMPTOM_FIELDS.filter(field => field.key !== 'pain');
    protected readonly symptomSeverityOptions: FdUiSegmentedToggleOption[] = [
        { value: SYMPTOM_SEVERITY_MILD_VALUE, labelKey: 'CYCLE_TRACKING.PAIN_LEVEL_3' },
        { value: SYMPTOM_SEVERITY_MODERATE_VALUE, labelKey: 'CYCLE_TRACKING.PAIN_LEVEL_6' },
        { value: SYMPTOM_SEVERITY_SEVERE_VALUE, labelKey: 'CYCLE_TRACKING.PAIN_LEVEL_9' },
    ];
    protected readonly advancedOpen = signal(false);
    protected readonly BLEEDING_TYPE_BLEEDING = BLEEDING_TYPE_BLEEDING;
    protected readonly BLEEDING_TYPE_SPOTTING = BLEEDING_TYPE_SPOTTING;
    protected readonly CYCLE_FLOW_LIGHT = CYCLE_FLOW_LIGHT;
    protected readonly CYCLE_FLOW_MEDIUM = CYCLE_FLOW_MEDIUM;
    protected readonly CYCLE_FLOW_HEAVY = CYCLE_FLOW_HEAVY;

    protected close(): void {
        this.advancedOpen.set(false);
        this.closed.emit();
    }

    protected setBleeding(type: BleedingType | null): void {
        this.dayForm()
            .isBleeding()
            .value.set(type !== null);
        if (type !== null) {
            this.dayForm().bleedingType().value.set(type);
        }
    }

    protected isBleedingSelected(type: BleedingType | null): boolean {
        const isBleeding = this.dayForm().isBleeding().value();
        return type === null ? !isBleeding : isBleeding && this.dayForm().bleedingType().value() === type;
    }

    protected setFlow(flow: CycleFlowLevel): void {
        this.dayForm().flow().value.set(flow);
    }

    protected setPain(value: number): void {
        this.dayForm().pain().value.set(value);
    }

    protected toggleSymptom(key: CycleSymptomField['key']): void {
        const field = this.symptomField(key)();
        field.value.set(field.value() > 0 ? 0 : SYMPTOM_SEVERITY_MILD);
    }

    protected isSymptomSelected(key: CycleSymptomField['key']): boolean {
        return this.symptomField(key)().value() > 0;
    }

    protected symptomSeverity(key: CycleSymptomField['key']): string {
        const value = this.symptomField(key)().value();
        if (value <= SYMPTOM_SEVERITY_MILD) {
            return SYMPTOM_SEVERITY_MILD_VALUE;
        }

        return value <= SYMPTOM_SEVERITY_MODERATE ? SYMPTOM_SEVERITY_MODERATE_VALUE : SYMPTOM_SEVERITY_SEVERE_VALUE;
    }

    protected setSymptomSeverity(key: CycleSymptomField['key'], value: string): void {
        this.symptomField(key)().value.set(Number(value));
    }

    protected symptomField(key: CycleSymptomField['key']): FieldTree<number> {
        return this.dayForm()[key];
    }
}
