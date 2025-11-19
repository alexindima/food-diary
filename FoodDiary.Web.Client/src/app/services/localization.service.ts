import { inject, Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { MeasurementUnit } from '../types/product.data';

@Injectable()
export class LocalizationService {
    private readonly translateService = inject(TranslateService);

    public initializeLocalization(): Promise<void> {
        this.translateService.addLangs(['en', 'ru']);
        this.translateService.setDefaultLang('en');

        const browserLang = this.translateService.getBrowserLang();
        const normalizedLang = this.normalizeLanguage(browserLang);

        return firstValueFrom(this.translateService.use(normalizedLang)).then(() => void 0);
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
