import { computed, DestroyRef, inject, Injector, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, EMPTY, finalize, from, type Observable, Subject, switchMap, takeUntil, throwError } from 'rxjs';

import { SessionEventsService } from '../../../../shared/auth/session-events.service';
import { type MealTypeOption, resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import { normalizeSatietyLevel } from '../../../../shared/lib/satiety-level.utils';
import type { Meal, MealItemManageDto, MealManageDto } from '../../../../shared/models/meal.data';
import type { Product } from '../../../../shared/models/product.data';
import type { Recipe } from '../../../../shared/models/recipe.data';
import { MAXIMUM_CONSUMED_QUANTITY } from '../../../../shared/models/semantics/meal-quantity';
import { MEAL_FEEDBACK_TOAST_KEY } from '../../../../shared/notifications/meal-feedback.config';
import { isHealthConsentRequiredError } from '../../../../shared/privacy/health-consent-error';
import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';
import { NutritionDataInvalidationService } from '../../../../shared/state/nutrition-data-invalidation.service';
import { MealService } from '../../api/meal.service';
import { getDateInputValue, getTimeInputValue, isExistingMealLocalDateTime } from '../meal-date-input.utils';
import { QUICK_MEAL_DEFAULT_ITEM_AMOUNT } from './quick-meal.config';

export type QuickMealItem = {
    key: string;
    amount: number;
    flashId?: number;
} & ({ type: 'product'; product: Product } | { type: 'recipe'; recipe: Recipe });

export type QuickMealDetails = {
    mealType?: MealTypeOption;
    date: string;
    time: string;
    comment: string;
    preMealSatietyLevel: number | null;
    postMealSatietyLevel: number | null;
};

@Service()
export class QuickMealService {
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly invalidation = inject(NutritionDataInvalidationService);
    private readonly resetDraft = new Subject<void>();
    private readonly mealService = inject(MealService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);

    private readonly itemsSignal = signal<QuickMealItem[]>([]);
    private readonly detailsSignal = signal<QuickMealDetails>(this.createDefaultDetails());
    private readonly isSavingSignal = signal(false);
    private nextFlashId = 0;
    private submittedAmounts = new WeakMap<QuickMealItem, number>();
    private isPreviewMode = false;
    private createOperation: { fingerprint: string; key: string } | null = null;

    public readonly items = computed(() => this.itemsSignal());
    public readonly details = computed(() => this.detailsSignal());
    public readonly hasItems = computed(() => this.itemsSignal().length > 0);
    public readonly isSaving = computed(() => this.isSavingSignal());

    public constructor() {
        this.sessionEvents.authenticated$.pipe(takeUntilDestroyed()).subscribe(() => {
            this.clearSessionDraft();
        });
        this.sessionEvents.sessionEnded$.pipe(takeUntilDestroyed()).subscribe(() => {
            this.clearSessionDraft();
        });
    }

    public addProduct(product: Product, preferredAmount?: number): void {
        if (product.id.length === 0) {
            return;
        }

        const amount = this.resolveProductAmount(product, preferredAmount);
        const key = `product-${product.id}`;
        this.refreshDetailsForFirstItem();
        this.upsertItem({
            key,
            type: 'product',
            product,
            amount,
        });

        if (this.isPreviewMode) {
            return;
        }

        this.toastService.success(this.translateService.instant('QUICK_MEAL.ADDED_PRODUCT'));
    }

    public addRecipe(recipe: Recipe, servings = QUICK_MEAL_DEFAULT_ITEM_AMOUNT): void {
        if (recipe.id.length === 0) {
            return;
        }

        const key = `recipe-${recipe.id}`;
        this.refreshDetailsForFirstItem();
        this.upsertItem({
            key,
            type: 'recipe',
            recipe,
            amount: Number.isFinite(servings) && servings > 0 ? servings : QUICK_MEAL_DEFAULT_ITEM_AMOUNT,
        });

        if (this.isPreviewMode) {
            return;
        }

        this.toastService.success(this.translateService.instant('QUICK_MEAL.ADDED_RECIPE'));
    }

    public removeItem(key: string): void {
        this.itemsSignal.update(items => items.filter(item => item.key !== key));
    }

    public updateItem(key: string, item: QuickMealItem): void {
        this.itemsSignal.update(items => {
            const flashId = ++this.nextFlashId;
            const withoutCurrent = items.filter(current => current.key !== key);
            const existingIndex = withoutCurrent.findIndex(current => current.key === item.key);

            if (existingIndex !== -1) {
                const updated = [...withoutCurrent];
                updated[existingIndex] = {
                    ...updated[existingIndex],
                    amount: updated[existingIndex].amount + item.amount,
                    flashId,
                };
                return updated;
            }

            const originalIndex = items.findIndex(current => current.key === key);
            const updatedItem = { ...item, flashId };
            if (originalIndex === -1) {
                return [...withoutCurrent, updatedItem];
            }

            const insertIndex = Math.min(originalIndex, withoutCurrent.length);
            return [...withoutCurrent.slice(0, insertIndex), updatedItem, ...withoutCurrent.slice(insertIndex)];
        });
    }

    public clear(): void {
        this.resetDraft.next();
        this.createOperation = null;
        this.itemsSignal.set([]);
        this.detailsSignal.set(this.createDefaultDetails());
    }

    public updateDetails(details: Partial<QuickMealDetails>): void {
        this.detailsSignal.update(current => ({
            ...current,
            ...details,
        }));
    }

    public saveDraft(): void {
        if (this.isPreviewMode) {
            return;
        }

        const items = this.itemsSignal();
        if (items.length === 0 || this.isSavingSignal()) {
            return;
        }

        const { date, time } = this.detailsSignal();
        if (!isExistingMealLocalDateTime(date, time)) {
            this.toastService.error(this.translateService.instant('MEAL_MANAGE.NONEXISTENT_TIME'));
            return;
        }

        if (items.some(item => item.amount > MAXIMUM_CONSUMED_QUANTITY)) {
            this.toastService.error(this.translateService.instant('MEAL_MANAGE.MAX_AMOUNT_ERROR', { max: MAXIMUM_CONSUMED_QUANTITY }));
            return;
        }

        const payload = this.toMealDto(items, new Date(`${date}T${time}`));
        const fingerprint = JSON.stringify(payload);
        if (this.createOperation?.fingerprint !== fingerprint) {
            this.createOperation = { fingerprint, key: crypto.randomUUID() };
        }
        const operation = this.createOperation;
        this.submittedAmounts = new WeakMap(items.map(item => [item, item.amount]));
        this.isSavingSignal.set(true);
        this.mealService
            .create(payload, operation.key)
            .pipe(
                catchError((error: unknown) =>
                    isHealthConsentRequiredError(error) ? this.retryAfterConsent(payload, operation.key) : throwError(() => error),
                ),
                takeUntil(this.resetDraft),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isSavingSignal.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.createOperation = null;
                    this.isSavingSignal.set(false);
                    this.toastService.success(this.translateService.instant('QUICK_MEAL.SAVE_SUCCESS'), { key: MEAL_FEEDBACK_TOAST_KEY });
                    this.invalidation.reportMealMutation();
                    this.removeSubmittedAmounts();
                    if (this.itemsSignal().length === 0) {
                        this.detailsSignal.set(this.createDefaultDetails());
                    }
                },
                error: () => {
                    this.isSavingSignal.set(false);
                    this.toastService.error(this.translateService.instant('QUICK_MEAL.SAVE_ERROR'));
                },
            });
    }

    private retryAfterConsent(payload: MealManageDto, idempotencyKey: string): Observable<Meal> {
        const privacy = this.injector.get(PrivacyConsentFacade);
        return from(privacy.ensureHealthConsentAsync(true, { acceptLabelKey: 'PRIVACY_CHOICES.ACCEPT_AND_SAVE' })).pipe(
            switchMap(allowed => {
                if (allowed) {
                    return this.mealService.create(payload, idempotencyKey);
                }
                if (privacy.failed()) {
                    this.toastService.error(
                        this.translateService.instant(privacy.needsReload() ? 'PRIVACY_CHOICES.RELOAD' : 'PRIVACY_CHOICES.ERROR'),
                    );
                }
                return EMPTY;
            }),
        );
    }

    public setPreviewItems(items: QuickMealItem[]): void {
        this.isPreviewMode = true;
        this.isSavingSignal.set(false);
        this.itemsSignal.set(items);
        this.detailsSignal.set(this.createDefaultDetails());
    }

    public exitPreview(): void {
        if (!this.isPreviewMode) {
            return;
        }

        this.clear();
        this.isPreviewMode = false;
    }

    private clearSessionDraft(): void {
        this.clear();
        this.isSavingSignal.set(false);
        this.isPreviewMode = false;
    }

    private resolveProductAmount(product: Product, preferredAmount?: number): number {
        if (preferredAmount !== undefined && Number.isFinite(preferredAmount) && preferredAmount > 0) {
            return preferredAmount;
        }

        if (Number.isFinite(product.defaultPortionAmount) && product.defaultPortionAmount > 0) {
            return product.defaultPortionAmount;
        }

        if (Number.isFinite(product.baseAmount) && product.baseAmount > 0) {
            return product.baseAmount;
        }

        return QUICK_MEAL_DEFAULT_ITEM_AMOUNT;
    }

    private refreshDetailsForFirstItem(): void {
        if (this.itemsSignal().length === 0) {
            this.detailsSignal.set(this.createDefaultDetails());
        }
    }

    private upsertItem(newItem: QuickMealItem): void {
        this.itemsSignal.update(items => {
            const flashId = ++this.nextFlashId;
            const existingIndex = items.findIndex(item => item.key === newItem.key);
            if (existingIndex !== -1) {
                const updated = [...items];
                updated[existingIndex] = {
                    ...updated[existingIndex],
                    amount: updated[existingIndex].amount + newItem.amount,
                    flashId,
                };
                const submitted = this.submittedAmounts.get(items[existingIndex]);
                if (submitted !== undefined) {
                    this.submittedAmounts.set(updated[existingIndex], submitted);
                }
                return updated;
            }

            return [...items, { ...newItem, flashId }];
        });
    }

    private removeSubmittedAmounts(): void {
        // New or edited entries belong to the next draft; merged additions retain only their unsaved portion.
        this.itemsSignal.update(items =>
            items.flatMap(item => {
                const submitted = this.submittedAmounts.get(item);
                if (submitted === undefined) {
                    return [item];
                }
                const amount = item.amount - submitted;
                return amount > 0 ? [{ ...item, amount }] : [];
            }),
        );
        this.submittedAmounts = new WeakMap<QuickMealItem, number>();
    }

    private toMealDto(items: QuickMealItem[], date: Date): MealManageDto {
        const mappedItems = items.map((item): MealItemManageDto =>
            item.type === 'product'
                ? { productId: item.product.id, recipeId: null, amount: item.amount }
                : { productId: null, recipeId: item.recipe.id, amount: item.amount },
        );

        return {
            date,
            mealType: this.detailsSignal().mealType ?? resolveMealTypeByTime(date),
            comment: this.resolveComment(),
            imageUrl: undefined,
            imageAssetId: undefined,
            items: mappedItems,
            isNutritionAutoCalculated: true,
            preMealSatietyLevel: normalizeSatietyLevel(this.detailsSignal().preMealSatietyLevel),
            postMealSatietyLevel: normalizeSatietyLevel(this.detailsSignal().postMealSatietyLevel),
        };
    }

    private createDefaultDetails(): QuickMealDetails {
        const now = new Date();
        return {
            date: getDateInputValue(now),
            mealType: resolveMealTypeByTime(now),
            time: getTimeInputValue(now),
            comment: '',
            preMealSatietyLevel: null,
            postMealSatietyLevel: null,
        };
    }

    private resolveComment(): string | undefined {
        const comment = this.detailsSignal().comment.trim();
        return comment.length > 0 ? comment : undefined;
    }
}
