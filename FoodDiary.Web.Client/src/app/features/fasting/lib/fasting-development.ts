import { devEnum, devNumber, devRecord } from '../../../shared/development/development-input';
import { connectDevelopmentScreen, DevelopmentToolError } from '../../../shared/development/development-tools';
import { DEFAULT_CHECK_IN_LEVEL, MAX_CYCLIC_DAYS, MAX_FASTING_HOURS, MAX_INTERMITTENT_FAST_HOURS } from '../../../shared/lib/fasting.constants';
import { extendedFastingHours, fastingCycleDays, fastingDailyWindow } from '../models/fasting-start-intent';
import type { FastingFacade } from './fasting.facade';

export function hasFastingDevelopmentDraft(facade: FastingFacade): boolean {
    const session = facade.currentSession();
    const baseline = session === null ? { hunger: DEFAULT_CHECK_IN_LEVEL, energy: DEFAULT_CHECK_IN_LEVEL, mood: DEFAULT_CHECK_IN_LEVEL, symptoms: [], notes: '' } : {
        hunger: session.hungerLevel ?? DEFAULT_CHECK_IN_LEVEL, energy: session.energyLevel ?? DEFAULT_CHECK_IN_LEVEL,
        mood: session.moodLevel ?? DEFAULT_CHECK_IN_LEVEL, symptoms: session.symptoms, notes: session.checkInNotes ?? '',
    };
    return JSON.stringify(baseline) !== JSON.stringify({ hunger: facade.hungerLevel(), energy: facade.energyLevel(), mood: facade.moodLevel(), symptoms: facade.selectedSymptoms(), notes: facade.checkInNotes() });
}

function readSetup(input: unknown): { mode: 'intermittent' | 'extended' | 'cyclic'; hours: number; fastDays: number | null; eatDays: number | null } {
    const value = devRecord(input, ['mode', 'fastHours', 'fastDays', 'eatDays']);
    const mode = devEnum(value['mode'], ['intermittent', 'extended', 'cyclic']);
    const hours = devNumber(value['fastHours'], 1, mode === 'extended' ? MAX_FASTING_HOURS : MAX_INTERMITTENT_FAST_HOURS);
    const fastDays = mode === 'cyclic' ? fastingCycleDays(devNumber(value['fastDays'], 1, MAX_CYCLIC_DAYS)) : null;
    const eatDays = mode === 'cyclic' ? fastingCycleDays(devNumber(value['eatDays'], 1, MAX_CYCLIC_DAYS)) : null;
    if (mode !== 'cyclic' && (value['fastDays'] !== undefined || value['eatDays'] !== undefined)) {
        throw new DevelopmentToolError('Cycle days apply only to cyclic setup.');
    }
    if (mode === 'extended') { extendedFastingHours(hours); } else { fastingDailyWindow(hours); }
    return { mode, hours, fastDays, eatDays };
}

export function readFastingDevelopmentContext(facade: FastingFacade): unknown {
    const session = facade.currentSession();
    return {
        loading: facade.isLoading(), active: facade.isActive(),
        session: session === null ? null : {
            id: session.id, status: session.status, protocol: session.protocol, planType: session.planType,
            occurrenceKind: session.occurrenceKind, startedAtUtc: session.startedAtUtc, endedAtUtc: session.endedAtUtc,
            plannedDurationHours: session.plannedDurationHours, initialPlannedDurationHours: session.initialPlannedDurationHours,
            addedDurationHours: session.addedDurationHours, cyclicFastDays: session.cyclicFastDays, cyclicEatDays: session.cyclicEatDays,
            cyclicEatDayFastHours: session.cyclicEatDayFastHours, cyclicEatDayEatingWindowHours: session.cyclicEatDayEatingWindowHours,
            cyclicPhaseDayNumber: session.cyclicPhaseDayNumber, cyclicPhaseDayTotal: session.cyclicPhaseDayTotal,
        },
    };
}

export function connectFastingDevelopmentSetup(facade: FastingFacade): void {
    const selections = (): Record<string, unknown> => ({
        mode: facade.selectedMode(), protocol: facade.selectedProtocol(), hours: facade.customHours(),
        intermittentFastHours: facade.customIntermittentFastHours(), fastDays: facade.cyclicFastDays(), eatDays: facade.cyclicEatDays(),
        eatDayProtocol: facade.cyclicEatDayProtocol(), eatDayFastHours: facade.cyclicEatDayFastHours(),
        customPreset: facade.cyclicUsesCustomPreset(),
    });
    // Capture through owner signals so clearing restores the exact setup that was open.
    const baseline = {
        mode: facade.selectedMode(), protocol: facade.selectedProtocol(), hours: facade.customHours(),
        intermittentFastHours: facade.customIntermittentFastHours(), fastDays: facade.cyclicFastDays(), eatDays: facade.cyclicEatDays(),
        eatDayProtocol: facade.cyclicEatDayProtocol(), eatDayFastHours: facade.cyclicEatDayFastHours(), customPreset: facade.cyclicUsesCustomPreset(),
    };
    const revision = (): string => JSON.stringify(selections());
    const originalRevision = revision();
    const requireReady = (): void => {
        if (facade.isActive() || facade.isLoading() || facade.isStarting()) {
            throw new DevelopmentToolError('An idle fasting setup is required.');
        }
    };
    connectDevelopmentScreen({
        kind: 'fasting-setup',
        read: () => ({ ...selections(), saved: false }),
        isDirty: () => revision() !== originalRevision,
        draftSchema: {
            type: 'object', additionalProperties: false, required: ['mode', 'fastHours'],
            properties: {
                mode: { enum: ['intermittent', 'extended', 'cyclic'] },
                fastHours: { type: 'integer', minimum: 1, maximum: MAX_FASTING_HOURS },
                fastDays: { type: 'integer', minimum: 1, maximum: MAX_CYCLIC_DAYS },
                eatDays: { type: 'integer', minimum: 1, maximum: MAX_CYCLIC_DAYS },
            },
        },
        fillDraft: input => {
            requireReady();
            const { mode, hours, fastDays, eatDays } = readSetup(input);
            // All fields validate before the first owner mutation.
            facade.selectMode(mode);
            if (mode === 'extended') {
                facade.selectProtocol('Custom'); facade.setCustomHours(hours);
            } else if (mode === 'intermittent') {
                facade.selectProtocol('CustomIntermittent'); facade.setCustomIntermittentFastHours(hours);
            } else if (fastDays !== null && eatDays !== null) {
                facade.setCyclicPreset(fastDays, eatDays); facade.setCyclicEatDayFastHours(hours);
            }
        },
        clearDraft: () => {
            requireReady();
            facade.selectMode(baseline.mode); facade.selectProtocol(baseline.protocol);
            facade.setCustomHours(baseline.hours); facade.setCustomIntermittentFastHours(baseline.intermittentFastHours);
            facade.setCyclicPreset(baseline.fastDays, baseline.eatDays);
            if (baseline.customPreset) { facade.selectCustomCyclicPreset(); }
            facade.setCyclicEatDayFastHours(baseline.eatDayFastHours); facade.selectCyclicEatDayProtocol(baseline.eatDayProtocol);
        },
    });
}
