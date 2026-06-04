import type { DietologistPermissions, DietologistRelationship } from '../../../../../shared/models/dietologist.data';
import { DEFAULT_DIETOLOGIST_PERMISSIONS } from './user-manage.config';
import type { DietologistFormValues } from './user-manage.types';

export function mapDietologistRelationshipToForm(relationship: DietologistRelationship | null): DietologistFormValues {
    if (relationship !== null) {
        return {
            email: relationship.email,
            ...relationship.permissions,
        };
    }

    return {
        email: '',
        ...DEFAULT_DIETOLOGIST_PERMISSIONS,
    };
}

export function getDietologistPermissions(value: DietologistFormValues): DietologistPermissions {
    return {
        shareProfile: value.shareProfile,
        shareMeals: value.shareMeals,
        shareStatistics: value.shareStatistics,
        shareWeight: value.shareWeight,
        shareWaist: value.shareWaist,
        shareGoals: value.shareGoals,
        shareHydration: value.shareHydration,
        shareFasting: value.shareFasting,
    };
}
