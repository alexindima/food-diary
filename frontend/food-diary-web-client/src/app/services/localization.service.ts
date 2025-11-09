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
        this.translateService.use(browserLang?.match(/en|ru/) ? browserLang : 'en');
    }

    public getServingUnitName(unit: MeasurementUnit): string {
        return this.translateService.instant(`FOOD_MANAGE.DEFAULT_SERVING_UNITS.${unit}`);
    }
}
