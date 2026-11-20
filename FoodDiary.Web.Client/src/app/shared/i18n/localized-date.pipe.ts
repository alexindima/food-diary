import { formatDate } from '@angular/common';
import { inject, Pipe, type PipeTransform } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

import { resolveTranslateLanguage } from './translate-language.utils';

@Pipe({
    name: 'localizedDate',
})
export class LocalizedDatePipe implements PipeTransform {
    private readonly translate = inject(TranslateService);

    public transform(
        value: Date | string | number | null | undefined,
        pattern = 'mediumDate',
        timezone?: string,
        locale?: string,
    ): string | undefined {
        if (value === null || value === undefined) {
            return undefined;
        }

        try {
            return formatDate(value, pattern, locale ?? resolveTranslateLanguage(this.translate), timezone);
        } catch {
            return undefined;
        }
    }
}
