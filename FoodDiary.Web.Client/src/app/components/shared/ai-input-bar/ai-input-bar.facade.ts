import { HttpStatusCode } from '@angular/common/http';
import { DestroyRef, inject, Injectable, signal, type WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, type Observable, of } from 'rxjs';

import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { getNumberProperty } from '../../../shared/lib/unknown-value.utils';
import type { FoodNutritionResponse, FoodVisionItem, FoodVisionResponse } from '../../../shared/models/ai.data';

@Injectable()
export class AiInputBarFacade {
    private readonly aiFoodFacade = inject(AiFoodFacade);
    private readonly destroyRef = inject(DestroyRef);

    public readonly text = createChannelState();
    public readonly photo = createChannelState();

    public analyzeText(text: string): void {
        this.runAnalysis(
            this.text,
            this.aiFoodFacade.parseFoodText({ text }),
            {
                premium: 'AI_INPUT_BAR.TEXT_ERROR_PREMIUM',
                quota: 'AI_INPUT_BAR.TEXT_ERROR_QUOTA',
                generic: 'AI_INPUT_BAR.TEXT_ERROR_GENERIC',
            },
            items => {
                this.calculateTextNutrition(items);
            },
        );
    }

    public analyzePhoto(assetId: string): void {
        this.runAnalysis(
            this.photo,
            this.aiFoodFacade.analyzeFoodImage({ imageAssetId: assetId }),
            {
                premium: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM',
                quota: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA',
                generic: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_GENERIC',
            },
            items => {
                this.calculatePhotoNutrition(items);
            },
        );
    }

    public calculateTextNutrition(items: FoodVisionItem[]): void {
        this.runNutrition(this.text, items, {
            quota: 'AI_INPUT_BAR.TEXT_ERROR_QUOTA',
            generic: 'AI_INPUT_BAR.TEXT_NUTRITION_ERROR',
        });
    }

    public calculatePhotoNutrition(items: FoodVisionItem[]): void {
        this.runNutrition(this.photo, items, {
            quota: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA',
            generic: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR',
        });
    }

    public clear(state: AiInputBarChannelState): void {
        state.analyzing.set(false);
        state.results.set([]);
        state.nutritionLoading.set(false);
        state.nutrition.set(null);
        state.errorKey.set(null);
        state.nutritionErrorKey.set(null);
    }

    public setEditResult(state: AiInputBarChannelState, items: FoodVisionItem[], nutrition: FoodNutritionResponse | null): void {
        state.results.set(items);
        if (items.length === 0) {
            this.setEmptyItemsError(state);
            return;
        }
        if (nutrition !== null) {
            state.nutritionLoading.set(false);
            state.nutritionErrorKey.set(null);
            state.nutrition.set(nutrition);
            return;
        }
        state === this.text ? this.calculateTextNutrition(items) : this.calculatePhotoNutrition(items);
    }

    private setEmptyItemsError(state: AiInputBarChannelState): void {
        state.results.set([]);
        state.nutritionLoading.set(false);
        state.nutrition.set(null);
        state.errorKey.set('AI_INPUT_BAR.EMPTY_ITEMS_ERROR');
        state.nutritionErrorKey.set(null);
    }

    private runAnalysis(
        state: AiInputBarChannelState,
        request$: Observable<FoodVisionResponse>,
        errorKeys: AnalysisErrorKeys,
        onItems: (items: FoodVisionItem[]) => void,
    ): void {
        state.analyzing.set(true);
        state.results.set([]);
        state.nutrition.set(null);
        state.errorKey.set(null);
        state.nutritionErrorKey.set(null);
        request$
            .pipe(
                catchError((error: unknown) => {
                    const status = getNumberProperty(error, 'status');
                    state.errorKey.set(
                        status === HttpStatusCode.Forbidden
                            ? errorKeys.premium
                            : status === HttpStatusCode.TooManyRequests
                              ? errorKeys.quota
                              : errorKeys.generic,
                    );
                    return of(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(response => {
                state.analyzing.set(false);
                if (response !== null) {
                    state.results.set(response.items);
                    if (response.items.length > 0) {
                        onItems(response.items);
                    }
                }
            });
    }

    private runNutrition(state: AiInputBarChannelState, items: FoodVisionItem[], errorKeys: NutritionErrorKeys): void {
        if (items.length === 0) {
            this.setEmptyItemsError(state);
            return;
        }
        state.nutritionLoading.set(true);
        state.nutrition.set(null);
        state.nutritionErrorKey.set(null);
        this.aiFoodFacade
            .calculateNutrition({ items })
            .pipe(
                catchError((error: unknown) => {
                    const status = getNumberProperty(error, 'status');
                    state.nutritionErrorKey.set(status === HttpStatusCode.TooManyRequests ? errorKeys.quota : errorKeys.generic);
                    return of(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(response => {
                state.nutritionLoading.set(false);
                if (response !== null) {
                    state.nutrition.set(response);
                }
            });
    }
}

export type AiInputBarChannelState = {
    analyzing: WritableSignal<boolean>;
    results: WritableSignal<FoodVisionItem[]>;
    nutritionLoading: WritableSignal<boolean>;
    nutrition: WritableSignal<FoodNutritionResponse | null>;
    errorKey: WritableSignal<string | null>;
    nutritionErrorKey: WritableSignal<string | null>;
};

type AnalysisErrorKeys = { premium: string; quota: string; generic: string };
type NutritionErrorKeys = { quota: string; generic: string };

function createChannelState(): AiInputBarChannelState {
    return {
        analyzing: signal(false),
        results: signal([]),
        nutritionLoading: signal(false),
        nutrition: signal(null),
        errorKey: signal(null),
        nutritionErrorKey: signal(null),
    };
}
