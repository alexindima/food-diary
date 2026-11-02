import { Pipe, type PipeTransform } from '@angular/core';

import { getLocalizedNumberFormatter } from './localized-number-formatter';

@Pipe({
    name: 'localizedNumber',
})
export class LocalizedNumberPipe implements PipeTransform {
    public transform(
        value: number | null | undefined,
        language: string,
        minimumFractionDigits = 0,
        maximumFractionDigits = minimumFractionDigits,
    ): string {
        if (value === null || value === undefined || !Number.isFinite(value)) {
            return '';
        }

        const formatted = getLocalizedNumberFormatter(language, minimumFractionDigits, maximumFractionDigits).format(value);
        return formatted.replace('-', '−');
    }
}
