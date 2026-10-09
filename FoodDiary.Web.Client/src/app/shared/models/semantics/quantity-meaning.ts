import type { SemanticNumber, UnbrandedNumber } from './number-meaning';

/** One marker prevents factories from accepting a different numeric role. */
export type SemanticQuantity<Meaning extends string> = SemanticNumber<Meaning>;
export type UnbrandedQuantity = UnbrandedNumber;
