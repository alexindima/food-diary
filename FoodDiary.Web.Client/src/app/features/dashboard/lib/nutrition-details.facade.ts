import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { catchError, EMPTY, finalize, map, Subject, switchMap } from 'rxjs';

import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { DashboardService } from '../api/dashboard.service';
import type { NutritionDetailsData } from '../models/nutrition-details.types';
import { getDashboardDateUtc } from './dashboard-date.utils';
import { loadNutritionDay, type NutritionDay } from './nutrition-details.loader';
import { hasMatchingNutritionTotal, nutritionDayProgress } from './nutrition-details.mapper';

type NutritionDetailsDay = NutritionDay & { progress: ReturnType<typeof nutritionDayProgress> };

@Injectable()
export class NutritionDetailsFacade {
    private readonly service = inject(DashboardService);
    private readonly translate = inject(TranslateService);
    private readonly data = inject<NutritionDetailsData>(FD_UI_DIALOG_DATA);
    private readonly reloadRequest = new Subject<void>();
    public readonly loading = signal(true);
    public readonly failed = signal(false);
    public readonly day = signal<NutritionDetailsDay | null>(null);

    public constructor() {
        this.reloadRequest
            .pipe(
                switchMap(() => {
                    this.loading.set(true);
                    this.failed.set(false);
                    this.day.set(null);
                    return loadNutritionDay(
                        {
                            date: getDashboardDateUtc(this.data.date),
                            timeZoneOffsetMinutes: -this.data.date.getTimezoneOffset(),
                            timeZoneId: new Intl.DateTimeFormat().resolvedOptions().timeZone,
                            locale: resolveTranslateLanguage(this.translate),
                        },
                        query => this.service.getSnapshotSilentlyStrict(query),
                    ).pipe(
                        map(day => {
                            const metric = this.data.metric;
                            const progress = nutritionDayProgress(day.snapshot, metric);
                            if (!hasMatchingNutritionTotal(day.meals, metric, progress.current)) {
                                throw new Error('Nutrition statistics and meals describe different versions of the day');
                            }
                            return { ...day, progress };
                        }),
                        catchError(() => {
                            this.failed.set(true);
                            return EMPTY;
                        }),
                        finalize(() => {
                            this.loading.set(false);
                        }),
                    );
                }),
                takeUntilDestroyed(inject(DestroyRef)),
            )
            .subscribe(day => {
                this.day.set(day);
            });
        this.reload();
    }

    public reload(): void {
        this.reloadRequest.next();
    }
}
