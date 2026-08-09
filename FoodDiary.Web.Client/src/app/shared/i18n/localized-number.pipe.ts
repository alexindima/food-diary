import { Pipe, type PipeTransform } from '@angular/core';

import { resolveAppLocale } from '../lib/locale.constants';

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

        return new Intl.NumberFormat(resolveAppLocale(language), { minimumFractionDigits, maximumFractionDigits }).format(value);
    }
}
