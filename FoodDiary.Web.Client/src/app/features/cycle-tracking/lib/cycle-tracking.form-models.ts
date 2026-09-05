import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import {
    BLEEDING_TYPE_BLEEDING,
    type BleedingType,
    CYCLE_FLOW_MEDIUM,
    type CycleFactorType,
    type CycleFlowLevel,
    type CycleReproductiveState,
    type CycleTrackingGoal,
    type CycleTrackingMode,
    type OvulationTestResult,
} from '../models/cycle.data';

export type StartCycleFormModel = {
    trackingStartDate: string | null;
    mode: CycleTrackingMode | null;
    averageCycleLength: number | null;
    averagePeriodLength: number | null;
    lutealLength: number | null;
    isRegular: boolean;
    showFertilityEstimates: boolean;
    discreetNotifications: boolean;
    goal: CycleTrackingGoal | null;
    reproductiveState: CycleReproductiveState | null;
    hideFromDashboard: boolean;
    cycleTrackingConsentGranted: boolean;
    nutritionInsightsConsentGranted: boolean;
    fertilitySignalsConsentGranted: boolean;
};

export type CycleSettingsFormModel = Omit<StartCycleFormModel, 'trackingStartDate'>;

export type CycleDayFormModel = {
    date: string | null;
    isBleeding: boolean;
    bleedingType: BleedingType | null;
    flow: CycleFlowLevel | null;
    pain: number;
    mood: number;
    energy: number;
    sleepQuality: number;
    appetite: number;
    craving: number;
    bloating: number;
    headache: number;
    skin: number;
    stool: number;
    nausea: number;
    libido: number;
    basalBodyTemperatureCelsius: number | null;
    ovulationTestResult: OvulationTestResult | null;
    cervicalFluid: string | null;
    hadSex: boolean;
    notes: string | null;
};

export function createDefaultCycleDayFormModel(): CycleDayFormModel {
    return {
        date: formatDateInputValue(new Date()),
        isBleeding: false,
        bleedingType: BLEEDING_TYPE_BLEEDING,
        flow: CYCLE_FLOW_MEDIUM,
        pain: 0,
        mood: 0,
        energy: 0,
        sleepQuality: 0,
        appetite: 0,
        craving: 0,
        bloating: 0,
        headache: 0,
        skin: 0,
        stool: 0,
        nausea: 0,
        libido: 0,
        basalBodyTemperatureCelsius: null,
        ovulationTestResult: null,
        cervicalFluid: null,
        hadSex: false,
        notes: null,
    };
}

export type CycleFactorFormModel = {
    type: CycleFactorType | null;
    startDate: string | null;
    endDate: string | null;
    notes: string | null;
};

export type MenstrualEpisodeFormModel = {
    startDate: string | null;
    endDate: string | null;
};
