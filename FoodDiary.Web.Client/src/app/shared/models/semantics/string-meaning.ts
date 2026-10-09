declare const stringMeaning: unique symbol;

/** One marker prevents explicit factories from accepting another branded string meaning. */
export type SemanticString<Meaning extends string> = string & { readonly [stringMeaning]: Meaning };
export type UnbrandedString = string & { readonly [stringMeaning]?: never };
