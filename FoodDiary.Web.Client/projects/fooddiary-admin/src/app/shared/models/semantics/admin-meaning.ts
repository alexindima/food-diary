/* eslint-disable no-redeclare -- overloads retain null and undefined boundary contracts */
/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- phantom meanings retain scalar SDK and fixture values */
declare const adminMeaning: unique symbol;
type Meaning<Name extends string> = string & { readonly [adminMeaning]: Name };
type RawString = string & { readonly [adminMeaning]?: never };

export type AdminId<Owner extends string> = Meaning<`admin-id:${Owner}`>;
export type AdminCalendarDate = Meaning<'admin-calendar-date'>;
export type AdminUtcInstant = Meaning<'admin-utc-instant'>;

export function adminId<Owner extends string>(value: RawString | AdminId<Owner>): AdminId<Owner> {
    return value as AdminId<Owner>;
}

export function optionalAdminId<Owner extends string>(value: RawString | AdminId<Owner> | null): AdminId<Owner> | null;
export function optionalAdminId<Owner extends string>(value: RawString | AdminId<Owner> | undefined): AdminId<Owner> | undefined;
export function optionalAdminId<Owner extends string>(
    value: RawString | AdminId<Owner> | null | undefined,
): AdminId<Owner> | null | undefined;
export function optionalAdminId<Owner extends string>(
    value: RawString | AdminId<Owner> | null | undefined,
): AdminId<Owner> | null | undefined {
    return value === null || value === undefined ? value : adminId<Owner>(value);
}

export function adminCalendarDate(value: RawString | AdminCalendarDate): AdminCalendarDate {
    return value as AdminCalendarDate;
}

export function optionalAdminCalendarDate(value: RawString | AdminCalendarDate | null): AdminCalendarDate | null;
export function optionalAdminCalendarDate(value: RawString | AdminCalendarDate | undefined): AdminCalendarDate | undefined;
export function optionalAdminCalendarDate(value: RawString | AdminCalendarDate | null | undefined): AdminCalendarDate | null | undefined;
export function optionalAdminCalendarDate(value: RawString | AdminCalendarDate | null | undefined): AdminCalendarDate | null | undefined {
    return value === null || value === undefined ? value : adminCalendarDate(value);
}

export function adminUtcInstant(value: RawString | AdminUtcInstant): AdminUtcInstant {
    return value as AdminUtcInstant;
}

export function optionalAdminUtcInstant(value: RawString | AdminUtcInstant | null): AdminUtcInstant | null;
export function optionalAdminUtcInstant(value: RawString | AdminUtcInstant | undefined): AdminUtcInstant | undefined;
export function optionalAdminUtcInstant(value: RawString | AdminUtcInstant | null | undefined): AdminUtcInstant | null | undefined;
export function optionalAdminUtcInstant(value: RawString | AdminUtcInstant | null | undefined): AdminUtcInstant | null | undefined {
    return value === null || value === undefined ? value : adminUtcInstant(value);
}
