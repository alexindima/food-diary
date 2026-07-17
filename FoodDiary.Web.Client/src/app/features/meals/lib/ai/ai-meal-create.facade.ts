import { computed, inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, finalize, type Observable, of, tap } from 'rxjs';

import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { NutritionDataInvalidationService } from '../../../../shared/state/nutrition-data-invalidation.service';
import type { Meal } from '../../models/meal.data';
import { AiMealCreateService } from './ai-meal-create.service';

@Injectable()
export class AiMealCreateFacade {
    private readonly aiMealCreateService = inject(AiMealCreateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private readonly invalidation = inject(NutritionDataInvalidationService);
    private readonly savingCount = signal(0);
    private readonly clearVersion = signal(0);

    public readonly isSaving = computed(() => this.savingCount() > 0);
    public readonly clearToken = this.clearVersion.asReadonly();
    public readonly errorKey = signal<string | null>(null);

    public createFromAiResult(result: AiInputBarResult): Observable<Meal | null> {
        if (this.isSaving()) {
            return of(null);
        }

        this.errorKey.set(null);
        this.savingCount.update(count => count + 1);

        return this.aiMealCreateService.createFromAiResult(result).pipe(
            tap(() => {
                this.invalidation.reportMealMutation();
                this.clearVersion.update(version => version + 1);
            }),
            catchError(() => {
                this.errorKey.set('AI_INPUT_BAR.CREATE_MEAL_ERROR');
                this.toastService.error(this.translateService.instant('AI_INPUT_BAR.CREATE_MEAL_ERROR'));
                return of(null);
            }),
            finalize(() => {
                this.savingCount.update(count => Math.max(0, count - 1));
            }),
        );
    }
}
