import {
    BLEEDING_TYPE_BLEEDING,
    type BleedingEntry,
    CYCLE_FLOW_MEDIUM,
    type CycleSymptomEntry,
    type FertilitySignal,
    type FertilitySignalPayload,
    OVULATION_TEST_RESULT_UNKNOWN,
    type SymptomLogPayload,
} from '../models/cycle.data';
import { CYCLE_SYMPTOM_FIELDS, MIN_SYMPTOM_VALUE } from './cycle-tracking.config';
import type { CycleDayFormModel } from './cycle-tracking.form-models';
import { clampCycleSymptom, toCycleDateKey, toNullableCycleNumber, toOptionalCycleText } from './cycle-tracking.mapper';

export function buildSymptomPayload(formValue: CycleDayFormModel): SymptomLogPayload[] {
    return CYCLE_SYMPTOM_FIELDS.map(field => ({ field, intensity: clampCycleSymptom(formValue[field.key]) }))
        .filter(item => item.intensity > MIN_SYMPTOM_VALUE)
        .map(item => ({
            category: item.field.category,
            intensity: item.intensity,
            tags: [],
            note: null,
            clearNote: false,
        }));
}

export function buildSymptomClearCategories(
    formValue: CycleDayFormModel,
    editingDate: string | null,
    symptoms: CycleSymptomEntry[],
): Array<CycleSymptomEntry['category']> {
    if (editingDate === null) {
        return [];
    }

    const existingCategories = new Set(
        symptoms.filter(symptom => toCycleDateKey(symptom.date) === toCycleDateKey(editingDate)).map(symptom => symptom.category),
    );

    return CYCLE_SYMPTOM_FIELDS.filter(
        field => existingCategories.has(field.category) && clampCycleSymptom(formValue[field.key]) === MIN_SYMPTOM_VALUE,
    ).map(field => field.category);
}

export function buildFertilitySignalPayload(formValue: CycleDayFormModel): FertilitySignalPayload | null {
    const basalBodyTemperatureCelsius = toNullableCycleNumber(formValue.basalBodyTemperatureCelsius);
    const cervicalFluid = toOptionalCycleText(formValue.cervicalFluid);
    const hasSignal =
        basalBodyTemperatureCelsius !== null || formValue.ovulationTestResult !== null || cervicalFluid !== undefined || formValue.hadSex;

    if (!hasSignal) {
        return null;
    }

    return {
        basalBodyTemperatureCelsius,
        ovulationTestResult: formValue.ovulationTestResult ?? OVULATION_TEST_RESULT_UNKNOWN,
        cervicalFluid,
        hadSex: formValue.hadSex,
        notes: undefined,
        clearNotes: false,
    };
}

export function buildDayEditModel(
    date: string,
    symptoms: CycleSymptomEntry[],
    bleeding: BleedingEntry | undefined,
    fertilitySignal: FertilitySignal | undefined,
): CycleDayFormModel {
    return {
        date: toCycleDateKey(date),
        ...buildBleedingEditFields(symptoms, bleeding),
        ...buildSymptomEditFields(symptoms),
        ...buildFertilityEditFields(fertilitySignal),
        notes: findDayNotes(bleeding, fertilitySignal),
    };
}

export function buildBleedingEditFields(
    symptoms: CycleSymptomEntry[],
    bleeding: BleedingEntry | undefined,
): Pick<CycleDayFormModel, 'isBleeding' | 'bleedingType' | 'flow' | 'pain'> {
    return {
        isBleeding: bleeding !== undefined,
        bleedingType: bleeding?.type ?? BLEEDING_TYPE_BLEEDING,
        flow: bleeding?.flow ?? CYCLE_FLOW_MEDIUM,
        pain: bleeding?.painImpact ?? findSymptomIntensity(symptoms, 'pain'),
    };
}

export function buildSymptomEditFields(
    symptoms: CycleSymptomEntry[],
): Pick<
    CycleDayFormModel,
    'mood' | 'energy' | 'sleepQuality' | 'appetite' | 'craving' | 'bloating' | 'headache' | 'skin' | 'stool' | 'nausea' | 'libido'
> {
    return {
        mood: findSymptomIntensity(symptoms, 'mood'),
        energy: findSymptomIntensity(symptoms, 'energy'),
        sleepQuality: findSymptomIntensity(symptoms, 'sleepQuality'),
        appetite: findSymptomIntensity(symptoms, 'appetite'),
        craving: findSymptomIntensity(symptoms, 'craving'),
        bloating: findSymptomIntensity(symptoms, 'bloating'),
        headache: findSymptomIntensity(symptoms, 'headache'),
        skin: findSymptomIntensity(symptoms, 'skin'),
        stool: findSymptomIntensity(symptoms, 'stool'),
        nausea: findSymptomIntensity(symptoms, 'nausea'),
        libido: findSymptomIntensity(symptoms, 'libido'),
    };
}

export function buildFertilityEditFields(
    fertilitySignal: FertilitySignal | undefined,
): Pick<CycleDayFormModel, 'basalBodyTemperatureCelsius' | 'ovulationTestResult' | 'cervicalFluid' | 'hadSex'> {
    return {
        basalBodyTemperatureCelsius: fertilitySignal?.basalBodyTemperatureCelsius ?? null,
        ovulationTestResult: fertilitySignal?.ovulationTestResult ?? null,
        cervicalFluid: fertilitySignal?.cervicalFluid ?? null,
        hadSex: fertilitySignal?.hadSex ?? false,
    };
}

export function findDayNotes(bleeding: BleedingEntry | undefined, fertilitySignal: FertilitySignal | undefined): string | null {
    return bleeding?.notes ?? fertilitySignal?.notes ?? null;
}

export function findSymptomIntensity(symptoms: CycleSymptomEntry[], key: (typeof CYCLE_SYMPTOM_FIELDS)[number]['key']): number {
    const symptomField = CYCLE_SYMPTOM_FIELDS.find(item => item.key === key);
    if (symptomField === undefined) {
        return MIN_SYMPTOM_VALUE;
    }

    return symptoms.find(symptom => symptom.category === symptomField.category)?.intensity ?? MIN_SYMPTOM_VALUE;
}
