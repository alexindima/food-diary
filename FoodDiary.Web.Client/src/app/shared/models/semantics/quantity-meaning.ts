declare const quantityMeaning: unique symbol;

/** One marker prevents factories from accepting a different numeric role. */
export type SemanticQuantity<Meaning extends string> = number & { readonly [quantityMeaning]: Meaning };
export type UnbrandedQuantity = number & { readonly [quantityMeaning]?: never };
