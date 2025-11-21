import { formatDate } from '@angular/common';
import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Pipe({
    name: 'localizedDate',
    standalone: true,
})
export class LocalizedDatePipe implements PipeTransform {
    private readonly translateService = inject(TranslateService);
    private locale = this.translateService.getCurrentLang() ?? 'en-US';

    public constructor() {
        this.translateService.onLangChange.subscribe(e => this.locale = e.lang);
    }

    public transform(value: Date | string | number | null | undefined, pattern = 'mediumDate'): string | undefined {
        if (value === null || value === undefined) {
            return undefined;
        }

        try {
            return formatDate(value as any, pattern, this.locale);
        } catch {
            return undefined;
        }
    }
}
