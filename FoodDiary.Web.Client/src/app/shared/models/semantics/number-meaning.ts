declare const numberMeaning: unique symbol;

/** One marker prevents constructors from accepting a different numeric meaning. */
export type SemanticNumber<Meaning extends string> = number & { readonly [numberMeaning]: Meaning };
export type UnbrandedNumber = number & { readonly [numberMeaning]?: never };
