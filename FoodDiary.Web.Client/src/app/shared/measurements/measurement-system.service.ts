import { inject, Service, signal } from '@angular/core';

import { BrowserStorageService } from '../platform/browser-storage.service';

const POUNDS_PER_KILOGRAM = 2.2046226218;
const CENTIMETERS_PER_INCH = 2.54;
const INCHES_PER_FOOT = 12;
const DECIMAL_BASE = 10;
const STORAGE_KEY = 'fd_measurement_system';

export type MeasurementSystem = 'metric' | 'imperial';

export type ImperialHeight = {
    feet: number;
    inches: number;
};

@Service()
export class MeasurementSystemService {
    private readonly storage = inject(BrowserStorageService);
    private readonly systemState = signal<MeasurementSystem>(this.readStoredSystem());

    public readonly system = this.systemState.asReadonly();

    public setSystem(system: MeasurementSystem): void {
        this.systemState.set(system);
        this.storage.setItem('local', STORAGE_KEY, system);
    }

    public displayWeight(weightKg: number, fractionDigits = 1): number {
        return this.system() === 'imperial' ? kilogramsToPounds(weightKg, fractionDigits) : round(weightKg, fractionDigits);
    }

    public canonicalWeight(displayWeight: number): number {
        return this.system() === 'imperial' ? poundsToKilograms(displayWeight) : displayWeight;
    }

    public displayLength(lengthCm: number, fractionDigits = 1): number {
        return this.system() === 'imperial' ? centimetersToInches(lengthCm, fractionDigits) : round(lengthCm, fractionDigits);
    }

    public canonicalLength(displayLength: number): number {
        return this.system() === 'imperial' ? inchesToCentimeters(displayLength) : displayLength;
    }

    public displayHeight(heightCm: number): ImperialHeight {
        return centimetersToImperialHeight(heightCm);
    }

    public canonicalHeight(feet: number, inches: number): number {
        return imperialHeightToCentimeters(feet, inches);
    }

    public weightUnitKey(): string {
        return this.system() === 'imperial' ? 'GENERAL.UNITS.LB' : 'GENERAL.UNITS.KG';
    }

    public lengthUnitKey(): string {
        return this.system() === 'imperial' ? 'GENERAL.UNITS.IN' : 'GENERAL.UNITS.CM';
    }

    private readStoredSystem(): MeasurementSystem {
        return this.storage.getItem('local', STORAGE_KEY) === 'imperial' ? 'imperial' : 'metric';
    }
}

export function kilogramsToPounds(weightKg: number, fractionDigits = 1): number {
    return round(weightKg * POUNDS_PER_KILOGRAM, fractionDigits);
}

export function poundsToKilograms(weightLb: number): number {
    return round(weightLb / POUNDS_PER_KILOGRAM, 2);
}

export function centimetersToInches(lengthCm: number, fractionDigits = 1): number {
    return round(lengthCm / CENTIMETERS_PER_INCH, fractionDigits);
}

export function inchesToCentimeters(lengthInches: number): number {
    return round(lengthInches * CENTIMETERS_PER_INCH, 2);
}

export function centimetersToImperialHeight(heightCm: number): ImperialHeight {
    const totalInches = Math.round(heightCm / CENTIMETERS_PER_INCH);
    return { feet: Math.floor(totalInches / INCHES_PER_FOOT), inches: totalInches % INCHES_PER_FOOT };
}

export function imperialHeightToCentimeters(feet: number, inches: number): number {
    return round((feet * INCHES_PER_FOOT + inches) * CENTIMETERS_PER_INCH, 1);
}

function round(value: number, fractionDigits: number): number {
    const factor = DECIMAL_BASE ** fractionDigits;
    return Math.round((value + Number.EPSILON) * factor) / factor;
}
