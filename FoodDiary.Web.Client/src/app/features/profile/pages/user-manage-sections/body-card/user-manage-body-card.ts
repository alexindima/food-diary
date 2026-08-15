import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import type { ActivityLevelOption } from '../../../../../shared/models/user.data';
import type { UserFormValues } from '../../user-manage/user-manage-lib/user-manage.types';

const IMPERIAL_HEIGHT_RANGES = {
    feet: { min: 1, max: 8 },
    inches: { min: 0, max: 11 },
} as const;

export type UserManageBodyFormPatch = Partial<Pick<UserFormValues, 'heightCm' | 'activityLevel'>>;

@Component({
    selector: 'fd-user-manage-body-card',
    imports: [FormField, TranslatePipe, FdUiCardComponent, FdUiInputComponent, FdUiSelectComponent],
    templateUrl: './user-manage-body-card.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageBodyCardComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly userForm = input.required<FieldTree<UserFormValues>>();
    public readonly activityLevelOptions = input.required<Array<FdUiSelectOption<ActivityLevelOption | null>>>();
    public readonly userFormPatch = output<UserManageBodyFormPatch>();

    protected readonly imperialHeight = computed(() => {
        const heightCm = this.userForm().heightCm().value();
        return heightCm === null ? { feet: null, inches: null } : this.measurements.displayHeight(heightCm);
    });

    protected onHeightChange(value: string | number | null): void {
        if (value === null || String(value).trim().length === 0) {
            this.emitFormPatch({ heightCm: null });
            return;
        }

        const parsed = Number(value);
        this.emitFormPatch({ heightCm: Number.isFinite(parsed) ? parsed : null });
    }

    protected onImperialHeightChange(part: 'feet' | 'inches', value: string | number | null): void {
        const parsed = this.parseImperialHeightPart(part, value);
        if (parsed === null) {
            this.emitFormPatch({ heightCm: null });
            return;
        }
        if (parsed === undefined) {
            return;
        }

        const current = this.imperialHeight();
        const feet = part === 'feet' ? parsed : (current.feet ?? 0);
        const inches = part === 'inches' ? parsed : (current.inches ?? 0);
        this.emitFormPatch({ heightCm: this.measurements.canonicalHeight(feet, inches) });
    }

    protected emitFormPatch(patch: UserManageBodyFormPatch): void {
        this.userFormPatch.emit(patch);
    }

    private parseImperialHeightPart(part: 'feet' | 'inches', value: string | number | null): number | null | undefined {
        if (value === null || String(value).trim().length === 0) {
            return null;
        }

        const parsed = Number(value);
        const range = IMPERIAL_HEIGHT_RANGES[part];
        return Number.isFinite(parsed) && parsed >= range.min && parsed <= range.max ? parsed : undefined;
    }
}
