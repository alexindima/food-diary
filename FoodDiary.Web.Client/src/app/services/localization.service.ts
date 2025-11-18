import { inject, Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MeasurementUnit } from '../types/product.data';

@Injectable()
export class LocalizationService {
    private readonly translateService = inject(TranslateService);

    public initializeLocalization(): void {
        this.translateService.addLangs(['en', 'ru']);
        this.translateService.setDefaultLang('en');

        const browserLang = this.translateService.getBrowserLang();
        const normalizedLang = this.normalizeLanguage(browserLang);

        this.translateService.use(normalizedLang);
    }

    public getServingUnitName(unit: MeasurementUnit): string {
        return this.translateService.instant(`PRODUCT_MANAGE.DEFAULT_SERVING_UNITS.${unit}`);
    }

    private normalizeLanguage(lang?: string | null): string {
        if (!lang) {
            return 'en';
        }

        const lower = lang.toLowerCase();
        if (lower.startsWith('ru')) {
            return 'ru';
        }

        if (lower.startsWith('en')) {
            return 'en';
        }

        return 'en';
    }
}
