import type { FormGroup } from '@angular/forms';

import type { DietologistPermissions, DietologistRelationship } from '../../../../shared/models/dietologist.data';
import { DEFAULT_DIETOLOGIST_PERMISSIONS } from './user-manage.config';
import type { DietologistFormData } from './user-manage.types';

export function syncDietologistFormFromRelationship(
    form: FormGroup<DietologistFormData>,
    relationship: DietologistRelationship | null,
): void {
    if (relationship !== null) {
        form.patchValue({
            email: relationship.email,
            ...relationship.permissions,
        });
        form.controls.email.disable({ emitEvent: false });
    } else {
        form.reset(
            {
                email: '',
                ...DEFAULT_DIETOLOGIST_PERMISSIONS,
            },
            { emitEvent: false },
        );
        form.controls.email.enable({ emitEvent: false });
    }

    form.markAsPristine();
    form.markAsUntouched();
}

export function getDietologistPermissions(form: FormGroup<DietologistFormData>): DietologistPermissions {
    return {
        shareProfile: form.controls.shareProfile.getRawValue(),
        shareMeals: form.controls.shareMeals.getRawValue(),
        shareStatistics: form.controls.shareStatistics.getRawValue(),
        shareWeight: form.controls.shareWeight.getRawValue(),
        shareWaist: form.controls.shareWaist.getRawValue(),
        shareGoals: form.controls.shareGoals.getRawValue(),
        shareHydration: form.controls.shareHydration.getRawValue(),
        shareFasting: form.controls.shareFasting.getRawValue(),
    };
}
