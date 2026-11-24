import { computed, inject, Injectable, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { map } from 'rxjs';

import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { DashboardService, type DashboardSnapshotQuery } from '../api/dashboard.service';
import type { NutritionDetailsData } from '../models/nutrition-details.types';
import { getDashboardDateUtc } from './dashboard-date.utils';
import { loadNutritionDay } from './nutrition-details.loader';
import { hasMatchingNutritionTotal, nutritionDayProgress } from './nutrition-details.mapper';

@Injectable()
export class NutritionDetailsFacade {
    private readonly service = inject(DashboardService);
    private readonly translate = inject(TranslateService);
    private readonly data = inject<NutritionDetailsData>(FD_UI_DIALOG_DATA);
    private readonly query = signal(this.createQuery());
    private readonly dayResource = rxResource({
        params: this.query,
        stream: ({ params }) =>
            loadNutritionDay(params, query => this.service.getSnapshotSilentlyStrict(query)).pipe(
                map(day => {
                    const metric = this.data.metric;
                    const progress = nutritionDayProgress(day.snapshot, metric);
                    if (!hasMatchingNutritionTotal(day.meals, metric, progress.current)) {
                        throw new Error('Nutrition statistics and meals describe different versions of the day');
                    }
                    return { ...day, progress };
                }),
            ),
    });

    public readonly loading = this.dayResource.isLoading;
    public readonly failed = computed(() => this.dayResource.error() !== undefined);
    public readonly day = computed(() => (this.dayResource.hasValue() ? this.dayResource.value() : null));

    public reload(): void {
        this.query.set(this.createQuery());
    }

    private createQuery(): DashboardSnapshotQuery {
        return {
            date: getDashboardDateUtc(this.data.date),
            timeZoneOffsetMinutes: -this.data.date.getTimezoneOffset(),
            timeZoneId: new Intl.DateTimeFormat().resolvedOptions().timeZone,
            locale: resolveTranslateLanguage(this.translate),
        };
    }
}
