import { MeasurementUnit } from '../models/product.data';

const UNITS: Readonly<Partial<Record<string, MeasurementUnit>>> = {
    G: MeasurementUnit.G,
    ML: MeasurementUnit.ML,
    PCS: MeasurementUnit.PCS,
};

/** API enum names (Ml/Pcs) must match the UI's canonical units (ML/PCS). */
export function normalizeProductUnit<T extends { baseUnit: string }>(product: T): T {
    const value = product.baseUnit.toUpperCase();
    const baseUnit = UNITS[value];
    return baseUnit === undefined ? product : { ...product, baseUnit };
}
