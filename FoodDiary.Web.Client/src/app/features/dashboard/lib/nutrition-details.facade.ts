import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { catchError, EMPTY, finalize, Subject, switchMap } from 'rxjs';

import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { DashboardService } from '../api/dashboard.service';
import type { NutritionDetailsData } from '../models/nutrition-details.types';
import { getDashboardDateUtc } from './dashboard-date.utils';
import { loadNutritionDay, type NutritionDay } from './nutrition-details.loader';
import { nutritionDayProgress } from './nutrition-details.mapper';

@Injectable()
export class NutritionDetailsFacade {
    private readonly service = inject(DashboardService);
    private readonly translate = inject(TranslateService);
    private readonly data = inject<NutritionDetailsData>(FD_UI_DIALOG_DATA);
    private readonly reloadRequest = new Subject<void>();
    public readonly loading = signal(true);
    public readonly failed = signal(false);
    public readonly day = signal<NutritionDay | null>(null);
    public readonly progress = computed(() => {
        const day = this.day();
        return day === null ? null : nutritionDayProgress(day.snapshot, this.data.metric);
    });

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
