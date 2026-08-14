import type { UserFormValues } from './user-manage.types';

const PROFILE_CALCULATION_FIELDS = 4;
const COMPLETE_PROFILE_PERCENTAGE = 100;

export const calculateProfileCompleteness = (
    values: Pick<UserFormValues, 'birthDate' | 'gender' | 'heightCm' | 'activityLevel'>,
): number => {
    const completedFields = [values.birthDate, values.gender, values.heightCm, values.activityLevel].filter(value => value !== null).length;

    return Math.round((completedFields / PROFILE_CALCULATION_FIELDS) * COMPLETE_PROFILE_PERCENTAGE);
};
