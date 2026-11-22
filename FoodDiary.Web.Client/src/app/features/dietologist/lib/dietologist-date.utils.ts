import { formatDateValue } from '../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import type { UtcInstant } from '../../../shared/models/semantics/date-value';

export function formatDietologistDate(value: UtcInstant, language: string): string {
    return formatDateValue(value, resolveAppLocale(language), { dateStyle: 'medium' }) ?? value;
}
