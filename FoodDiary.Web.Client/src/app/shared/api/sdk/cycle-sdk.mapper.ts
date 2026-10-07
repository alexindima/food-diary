import type {
    BleedingEntry,
    CycleLogDay,
    CycleNutritionSummary,
    CycleResponse,
    CycleSymptomEntry,
    FertilitySignal,
} from '../../models/cycle.data';
import type { CyclePredictionRevision, CyclePredictions } from '../../models/cycle.data';
import * as cycleValues from '../../models/cycle.data';
import type { BleedingEntryHttpResponse } from './generated/model/bleeding-entry-http-response';
import type { CycleHttpResponse } from './generated/model/cycle-http-response';
import type { CycleLogDayHttpResponse } from './generated/model/cycle-log-day-http-response';
import type { CycleNutritionSummaryHttpResponse } from './generated/model/cycle-nutrition-summary-http-response';
import type { CyclePredictionRevisionHttpResponse } from './generated/model/cycle-prediction-revision-http-response';
import type { CyclePredictionsHttpResponse } from './generated/model/cycle-predictions-http-response';
import type { CycleSymptomEntryHttpResponse } from './generated/model/cycle-symptom-entry-http-response';
import type { FertilitySignalHttpResponse } from './generated/model/fertility-signal-http-response';
import { requireSdkFields, sdkEnum, sdkMaybe } from './sdk-response';

function bleedingFromSdk(response: BleedingEntryHttpResponse): BleedingEntry {
    const value = requireSdkFields(response, ['id', 'cycleProfileId', 'date', 'type', 'flow']);
    return {
        ...value,
        type: sdkEnum(value.type, [0, 1] as const),
        flow: sdkEnum(value.flow, [
            cycleValues.CYCLE_FLOW_NONE,
            cycleValues.CYCLE_FLOW_LIGHT,
            cycleValues.CYCLE_FLOW_MEDIUM,
            cycleValues.CYCLE_FLOW_HEAVY,
        ] as const),
    };
}

function symptomFromSdk(response: CycleSymptomEntryHttpResponse): CycleSymptomEntry {
    const value = requireSdkFields(response, ['id', 'cycleProfileId', 'date', 'category', 'intensity', 'tags']);
    return {
        ...value,
        category: sdkEnum(value.category, [
            cycleValues.CYCLE_SYMPTOM_CATEGORY_PAIN,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_MOOD,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_ENERGY,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_SLEEP,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_APPETITE,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_CRAVING,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_BLOATING,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_HEADACHE,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_SKIN,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_STOOL,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_NAUSEA,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_LIBIDO,
            cycleValues.CYCLE_SYMPTOM_CATEGORY_OTHER,
        ] as const),
    };
}

function fertilityFromSdk(response: FertilitySignalHttpResponse): FertilitySignal {
    const value = requireSdkFields(response, ['id', 'cycleProfileId', 'date']);
    return { ...value, ovulationTestResult: sdkMaybe(value.ovulationTestResult, result => sdkEnum(result, [0, 1, 2] as const)) };
}

export function cycleDayFromSdk(response: CycleLogDayHttpResponse): CycleLogDay {
    const value = requireSdkFields(response, ['cycleProfileId', 'date', 'bleedingEntries', 'symptoms']);
    return {
        ...value,
        bleedingEntries: value.bleedingEntries.map(bleedingFromSdk),
        symptoms: value.symptoms.map(symptomFromSdk),
        fertilitySignal: sdkMaybe(value.fertilitySignal, fertilityFromSdk),
    };
}

/** All cycle days and prediction boundaries remain calendar-date strings. */
export function cycleFromSdk(response: CycleHttpResponse): CycleResponse {
    const value = requireSdkFields(response, [
        'id',
        'userId',
        'mode',
        'confidence',
        'trackingStartDate',
        'averageCycleLength',
        'averagePeriodLength',
        'lutealLength',
        'isRegular',
        'isOnboardingComplete',
        'showFertilityEstimates',
        'discreetNotifications',
        'goal',
        'reproductiveState',
        'hideFromDashboard',
        'bleedingEntries',
        'symptoms',
        'factors',
        'fertilitySignals',
    ]);
    return {
        ...value,
        mode: sdkEnum(value.mode, [
            cycleValues.CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            cycleValues.CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
            cycleValues.CYCLE_TRACKING_MODE_PREGNANCY,
            cycleValues.CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
            cycleValues.CYCLE_TRACKING_MODE_PERIMENOPAUSE,
            cycleValues.CYCLE_TRACKING_MODE_NO_PERIOD,
        ] as const),
        confidence: sdkEnum(value.confidence, [
            cycleValues.CYCLE_CONFIDENCE_LEARNING,
            cycleValues.CYCLE_CONFIDENCE_LOW,
            cycleValues.CYCLE_CONFIDENCE_MEDIUM,
            cycleValues.CYCLE_CONFIDENCE_HIGH,
        ] as const),
        goal: sdkEnum(value.goal, [0, 1, 2] as const),
        reproductiveState: sdkEnum(value.reproductiveState, [
            cycleValues.CYCLE_REPRODUCTIVE_STATE_CYCLING,
            cycleValues.CYCLE_REPRODUCTIVE_STATE_PREGNANCY,
            cycleValues.CYCLE_REPRODUCTIVE_STATE_POSTPARTUM,
            cycleValues.CYCLE_REPRODUCTIVE_STATE_LACTATION,
            cycleValues.CYCLE_REPRODUCTIVE_STATE_PERIMENOPAUSE,
            cycleValues.CYCLE_REPRODUCTIVE_STATE_NO_PERIOD,
        ] as const),
        bleedingEntries: value.bleedingEntries.map(bleedingFromSdk),
        symptoms: value.symptoms.map(symptomFromSdk),
        factors: value.factors.map(responseFactor => {
            const factor = requireSdkFields(responseFactor, ['id', 'cycleProfileId', 'type', 'startDate']);
            return {
                ...factor,
                type: sdkEnum(factor.type, [
                    cycleValues.CYCLE_FACTOR_TYPE_PREGNANCY,
                    cycleValues.CYCLE_FACTOR_TYPE_LACTATION,
                    cycleValues.CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
                    cycleValues.CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
                    cycleValues.CYCLE_FACTOR_TYPE_POSTPARTUM,
                    cycleValues.CYCLE_FACTOR_TYPE_PERIMENOPAUSE,
                    cycleValues.CYCLE_FACTOR_TYPE_NO_PERIOD,
                ] as const),
            };
        }),
        fertilitySignals: value.fertilitySignals.map(fertilityFromSdk),
        menstrualEpisodes: value.menstrualEpisodes?.map(episode =>
            requireSdkFields(episode, ['id', 'cycleProfileId', 'startDate', 'status', 'excludedFromPredictions']),
        ),
        consents: value.consents?.map(responseConsent => {
            const consent = requireSdkFields(responseConsent, ['id', 'purpose', 'grantedAtUtc', 'isActive']);
            return { ...consent, purpose: sdkEnum(consent.purpose, [0, 1, 2] as const) };
        }),
        dayNotes: value.dayNotes?.map(note => requireSdkFields(note, ['date', 'notes'])),
        predictions: sdkMaybe(value.predictions, predictionsFromSdk),
        predictionRevisions: value.predictionRevisions?.map(predictionRevisionFromSdk),
    };
}

function predictionsFromSdk(response: CyclePredictionsHttpResponse): CyclePredictions {
    const value = requireSdkFields(response, ['confidence', 'rationale']);
    return {
        ...value,
        dataSufficiency: value.dataSufficiency ?? undefined,
        patternConsistency: value.patternConsistency ?? undefined,
        reasonCodes: value.reasonCodes ?? undefined,
        algorithmVersion: value.algorithmVersion ?? undefined,
    };
}

function predictionRevisionFromSdk(value: CyclePredictionRevisionHttpResponse): CyclePredictionRevision {
    return requireSdkFields(value, [
        'id',
        'generatedAtUtc',
        'confidence',
        'dataSufficiency',
        'patternConsistency',
        'completedCycleCount',
        'calibrationSampleCount',
        'reasonCodes',
        'algorithmVersion',
    ]);
}

export function cycleNutritionFromSdk(response: CycleNutritionSummaryHttpResponse): CycleNutritionSummary {
    const value = requireSdkFields(response, [
        'dateFrom',
        'dateTo',
        'loggedCycleDays',
        'daysWithMeals',
        'bleedingDays',
        'averageCaloriesOnBleedingDays',
        'averageCaloriesOnNonBleedingCycleDays',
        'averageFiberOnBleedingDays',
        'averageFiberOnNonBleedingCycleDays',
        'averagePainImpactOnDaysWithMeals',
        'hasEnoughNutritionData',
    ]);
    return {
        ...value,
        dataSufficiency: value.dataSufficiency ?? undefined,
        reasonCodes: value.reasonCodes ?? undefined,
        algorithmVersion: value.algorithmVersion ?? undefined,
    };
}
