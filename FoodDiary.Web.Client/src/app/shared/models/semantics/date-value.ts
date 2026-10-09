/* eslint-disable no-redeclare -- optional factory overloads preserve the caller's null versus undefined contract */
/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- these constructors attach a phantom meaning without changing scalar wire values */
import type { SemanticString, UnbrandedString } from './string-meaning';

export type CalendarDate = SemanticString<'calendar-date'>;
export type UtcInstant = SemanticString<'utc-instant'>;
type UnbrandedDate = UnbrandedString;

/** Preserve the existing date-only or UTC-midnight wire encoding verbatim. */
export function calendarDate(value: UnbrandedDate | CalendarDate): CalendarDate {
    return value as CalendarDate;
}

/** Retain source precision and encoding; explicit UI conversions belong to date helpers. */
export function utcInstant(value: UnbrandedDate | UtcInstant): UtcInstant {
    return value as UtcInstant;
}

export function optionalCalendarDate(value: null): null;
export function optionalCalendarDate(value: undefined): undefined;
export function optionalCalendarDate(value: UnbrandedDate | CalendarDate | null): CalendarDate | null;
export function optionalCalendarDate(value: UnbrandedDate | CalendarDate | undefined): CalendarDate | undefined;
export function optionalCalendarDate(value: UnbrandedDate | CalendarDate | null | undefined): CalendarDate | null | undefined;
export function optionalCalendarDate(value: UnbrandedDate | CalendarDate | null | undefined): CalendarDate | null | undefined {
    return value === null || value === undefined ? value : calendarDate(value);
}

export function optionalUtcInstant(value: null): null;
export function optionalUtcInstant(value: undefined): undefined;
export function optionalUtcInstant(value: UnbrandedDate | UtcInstant | null): UtcInstant | null;
export function optionalUtcInstant(value: UnbrandedDate | UtcInstant | undefined): UtcInstant | undefined;
export function optionalUtcInstant(value: UnbrandedDate | UtcInstant | null | undefined): UtcInstant | null | undefined;
export function optionalUtcInstant(value: UnbrandedDate | UtcInstant | null | undefined): UtcInstant | null | undefined {
    return value === null || value === undefined ? value : utcInstant(value);
}
