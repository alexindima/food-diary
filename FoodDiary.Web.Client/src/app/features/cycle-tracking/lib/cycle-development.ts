import type { FieldTree } from '@angular/forms/signals';

import { devNumber, devRecord } from '../../../shared/development/development-input';
import { type DevelopmentScreen, DevelopmentToolError } from '../../../shared/development/development-tools';
import {
    MAX_AVERAGE_CYCLE_LENGTH,
    MAX_AVERAGE_PERIOD_LENGTH,
    MAX_LUTEAL_LENGTH,
    MIN_AVERAGE_CYCLE_LENGTH,
    MIN_AVERAGE_PERIOD_LENGTH,
    MIN_LUTEAL_LENGTH,
} from './cycle-tracking.config';
import type { CycleSettingsFormModel } from './cycle-tracking.form-models';

const DURATIONS = [
    {
        name: 'averageCycleLength',
        min: MIN_AVERAGE_CYCLE_LENGTH,
        max: MAX_AVERAGE_CYCLE_LENGTH,
        errorKey: 'CYCLE_TRACKING.AVG_LENGTH_ERROR',
    },
    {
        name: 'averagePeriodLength',
        min: MIN_AVERAGE_PERIOD_LENGTH,
        max: MAX_AVERAGE_PERIOD_LENGTH,
        errorKey: 'CYCLE_TRACKING.PERIOD_LENGTH_ERROR',
    },
    { name: 'lutealLength', min: MIN_LUTEAL_LENGTH, max: MAX_LUTEAL_LENGTH, errorKey: 'CYCLE_TRACKING.LUTEAL_LENGTH_ERROR' },
] as const;

/** Publish only settings metadata; clinical observations and consent values stay private. */
export function cycleSettingsDevelopmentScreen(
    form: FieldTree<CycleSettingsFormModel>,
    {
        busy,
        close,
        error,
        translate,
    }: { busy: () => boolean; close: () => void; error: () => string | null; translate: (key: string) => string },
): DevelopmentScreen {
    const original = { ...form().value() };
    const names = DURATIONS.map(field => field.name);
    const durationNames = new Set<string>(names);
    const baseline = new Map(Object.entries(original));
    const requireIdle = (): void => {
        if (busy() || form().disabled()) {
            throw new DevelopmentToolError('Cycle settings are busy.');
        }
    };
    return {
        kind: 'cycle-settings',
        isDirty: () => form().dirty(),
        read: () => ({
            mode: form.mode().value(),
            values: Object.fromEntries(DURATIONS.map(field => [field.name, form[field.name]().value()])),
            valid: form().valid(),
            busy: busy(),
            error: error(),
            fields: readDurationFields(form, translate),
        }),
        draftSchema: {
            type: 'object',
            additionalProperties: false,
            description:
                'Three integer durations only. Pass the current expectedRevision to fill_current_draft. No save or consent changes.',
            required: names,
            properties: Object.fromEntries(
                DURATIONS.map(field => [field.name, { type: 'integer', minimum: field.min, maximum: field.max }]),
            ),
        },
        fillDraft: (input): void => {
            requireIdle();
            const raw = devRecord(input, names);
            const values = Object.fromEntries(
                DURATIONS.map(field => {
                    const value = devNumber(raw[field.name], field.min, field.max);
                    if (!Number.isInteger(value)) {
                        throw new DevelopmentToolError('Cycle durations must be whole days.');
                    }
                    return [field.name, value];
                }),
            );
            form().value.set({ ...form().value(), ...values });
            form().markAsDirty();
        },
        clearDraft: (): void => {
            requireIdle();
            const current = form().value();
            if (Object.entries(current).some(([key, value]) => !durationNames.has(key) && baseline.get(key) !== value)) {
                throw new DevelopmentToolError('Other settings changed. Review or cancel them through the interface.');
            }
            form().reset({ ...original });
        },
        beforeNavigate: (): void => {
            requireIdle();
            close();
        },
    };
}

function readDurationFields(form: FieldTree<CycleSettingsFormModel>, translate: (key: string) => string): unknown {
    return Object.fromEntries(
        DURATIONS.map(field => [
            field.name,
            {
                min: field.min,
                max: field.max,
                step: 1,
                unit: 'days',
                valid: form[field.name]().valid(),
                dirty: form[field.name]().dirty(),
                touched: form[field.name]().touched(),
                errors: form[field.name]()
                    .errors()
                    .map(item => item.kind),
                validationMessage: form[field.name]().invalid() ? translate(field.errorKey) : null,
            },
        ]),
    );
}
