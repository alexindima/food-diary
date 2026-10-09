import type { MenstrualEpisode } from '../../models/cycle.data';
import type { CycleFactor } from '../../models/cycle.data';
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
import { calendarDate, optionalCalendarDate, optionalUtcInstant, utcInstant } from '../../models/semantics/date-value';
import { entityId } from '../../models/semantics/entity-id';
import type { BleedingEntryHttpResponse } from './generated/model/bleeding-entry-http-response';
import type { CycleFactorHttpResponse } from './generated/model/cycle-factor-http-response';
import type { CycleHttpResponse } from './generated/model/cycle-http-response';
import type { CycleLogDayHttpResponse } from './generated/model/cycle-log-day-http-response';
import type { CycleNutritionSummaryHttpResponse } from './generated/model/cycle-nutrition-summary-http-response';
import type { CyclePredictionRevisionHttpResponse } from './generated/model/cycle-prediction-revision-http-response';
import type { CyclePredictionsHttpResponse } from './generated/model/cycle-predictions-http-response';
import type { CycleSymptomEntryHttpResponse } from './generated/model/cycle-symptom-entry-http-response';
import type { FertilitySignalHttpResponse } from './generated/model/fertility-signal-http-response';
import type { MenstrualEpisodeHttpResponse } from './generated/model/menstrual-episode-http-response';
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

        id: entityId<'bleeding-entry'>(value.id),
        cycleProfileId: entityId<'cycle-profile'>(value.cycleProfileId),
        date: calendarDate(value.date),
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

        id: entityId<'cycle-symptom-entry'>(value.id),
        cycleProfileId: entityId<'cycle-profile'>(value.cycleProfileId),
        date: calendarDate(value.date),
    };
}

function fertilityFromSdk(response: FertilitySignalHttpResponse): FertilitySignal {
    const value = requireSdkFields(response, ['id', 'cycleProfileId', 'date']);
    return {
        ...value,
        ovulationTestResult: sdkMaybe(value.ovulationTestResult, result => sdkEnum(result, [0, 1, 2] as const)),
        id: entityId<'fertility-signal'>(value.id),
        cycleProfileId: entityId<'cycle-profile'>(value.cycleProfileId),
        date: calendarDate(value.date),
    };
}

export function cycleDayFromSdk(response: CycleLogDayHttpResponse): CycleLogDay {
    const value = requireSdkFields(response, ['cycleProfileId', 'date', 'bleedingEntries', 'symptoms']);
    return {
        ...value,
        bleedingEntries: value.bleedingEntries.map(bleedingFromSdk),
        symptoms: value.symptoms.map(symptomFromSdk),
        fertilitySignal: sdkMaybe(value.fertilitySignal, fertilityFromSdk),

        cycleProfileId: entityId<'cycle-profile'>(value.cycleProfileId),
        date: calendarDate(value.date),
    };
}

function factorFromSdk(responseFactor: CycleFactorHttpResponse): CycleFactor {
    const factor = requireSdkFields(responseFactor, ['id', 'cycleProfileId', 'type', 'startDate']);
    return {
        ...factor,
        id: entityId<'cycle-factor'>(factor.id),
        cycleProfileId: entityId<'cycle-profile'>(factor.cycleProfileId),
        startDate: calendarDate(factor.startDate),
        endDate: optionalCalendarDate(factor.endDate),
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
}

function episodeFromSdk(episode: MenstrualEpisodeHttpResponse): MenstrualEpisode {
    const mapped = requireSdkFields(episode, ['id', 'cycleProfileId', 'startDate', 'status', 'excludedFromPredictions']);
    return {
        ...mapped,
        id: entityId<'menstrual-episode'>(mapped.id),
        cycleProfileId: entityId<'cycle-profile'>(mapped.cycleProfileId),
        startDate: calendarDate(mapped.startDate),
        endDate: optionalCalendarDate(mapped.endDate),
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
        factors: value.factors.map(factorFromSdk),
        fertilitySignals: value.fertilitySignals.map(fertilityFromSdk),
        menstrualEpisodes: value.menstrualEpisodes?.map(episodeFromSdk),
        consents: value.consents?.map(responseConsent => {
            const consent = requireSdkFields(responseConsent, ['id', 'purpose', 'grantedAtUtc', 'isActive']);
            return {
                ...consent,
                id: entityId<'cycle-consent'>(consent.id),
                grantedAtUtc: utcInstant(consent.grantedAtUtc),
                revokedAtUtc: optionalUtcInstant(consent.revokedAtUtc),
                purpose: sdkEnum(consent.purpose, [0, 1, 2] as const),
            };
        }),
        dayNotes: value.dayNotes?.map(note => {
            const mapped = requireSdkFields(note, ['date', 'notes']);
            return { ...mapped, date: calendarDate(mapped.date) };
        }),
        predictions: sdkMaybe(value.predictions, predictionsFromSdk),
        predictionRevisions: value.predictionRevisions?.map(predictionRevisionFromSdk),

        id: entityId<'cycle-profile'>(value.id),
        userId: entityId<'user'>(value.userId),
        trackingStartDate: calendarDate(value.trackingStartDate),
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

        nextPeriodStartFrom: optionalCalendarDate(value.nextPeriodStartFrom),
        nextPeriodStartTo: optionalCalendarDate(value.nextPeriodStartTo),
        ovulationFrom: optionalCalendarDate(value.ovulationFrom),
        ovulationTo: optionalCalendarDate(value.ovulationTo),
        pmsWindowStart: optionalCalendarDate(value.pmsWindowStart),
        pmsWindowEnd: optionalCalendarDate(value.pmsWindowEnd),
    };
}

function predictionRevisionFromSdk(value: CyclePredictionRevisionHttpResponse): CyclePredictionRevision {
    const mapped = requireSdkFields(value, [
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
    return {
        ...mapped,
        id: entityId<'cycle-prediction-revision'>(mapped.id),
        generatedAtUtc: utcInstant(mapped.generatedAtUtc),
        nextPeriodStartFrom: optionalCalendarDate(mapped.nextPeriodStartFrom),
        nextPeriodStartTo: optionalCalendarDate(mapped.nextPeriodStartTo),
    };
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

        dateFrom: calendarDate(value.dateFrom),
        dateTo: calendarDate(value.dateTo),
    };
}
