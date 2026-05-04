import { formatDate } from '@angular/common';
import { DestroyRef, inject, Pipe, type PipeTransform } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

@Pipe({
    name: 'localizedDate',
    standalone: true,
})
export class LocalizedDatePipe implements PipeTransform {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private locale = this.translateService.getCurrentLang();

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(e => (this.locale = e.lang));
    }

    public transform(value: Date | string | number | null | undefined, pattern = 'mediumDate'): string | undefined {
        if (value === null || value === undefined) {
            return undefined;
        }

        try {
            return formatDate(value, pattern, this.locale);
        } catch {
            return undefined;
        }
    }
}
