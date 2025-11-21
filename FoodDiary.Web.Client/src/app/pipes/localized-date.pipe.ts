import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Pipe({
    name: 'localizedDate',
    standalone: true,
})
export class LocalizedDatePipe implements PipeTransform {
    private readonly translateService = inject(TranslateService);
    private readonly datePipe = inject(DatePipe);

    private locale = this.translateService.getCurrentLang() ?? 'en-US';

    public constructor() {
        this.translateService.onLangChange.subscribe(e => {
            this.locale = e.lang;
        });
    }

    public transform(value: Date | string | null | undefined, pattern = 'mediumDate'): string | undefined {
        if (!value) {
            return undefined;
        }

        return this.datePipe.transform(value, pattern, undefined, this.locale) ?? undefined;
    }
}
