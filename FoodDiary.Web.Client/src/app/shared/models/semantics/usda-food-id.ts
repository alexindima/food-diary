/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- attach the existing external key meaning without changing wire values or adding validation */
import type { SemanticNumber, UnbrandedNumber } from './number-meaning';

export type UsdaFoodId = SemanticNumber<'external-id:usda-food'>;

export function usdaFoodId(value: UnbrandedNumber | UsdaFoodId): UsdaFoodId {
    return value as UsdaFoodId;
}
