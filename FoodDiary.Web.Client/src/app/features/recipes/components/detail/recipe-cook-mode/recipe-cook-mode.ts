import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog';

import { injectCurrentLanguage } from '../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../shared/i18n/localized-number.pipe';
import { capitalizeFirstLetter } from '../../../../../shared/lib/text-display.utils';
import type { Recipe, RecipeIngredient, RecipeStep } from '../../../../../shared/models/recipe.data';
import { resolveIngredientUnitKey } from '../../../lib/recipe-servings.utils';

const PERCENT_SCALE = 100;

type CookModeIngredientView = {
    name: string;
    amount: number | string;
    unitKey: string;
};

@Component({
    selector: 'fd-recipe-cook-mode',
    templateUrl: './recipe-cook-mode.html',
    styleUrls: ['./recipe-cook-mode.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent, LocalizedNumberPipe],
})
export class RecipeCookModeComponent {
    private readonly dialogService = inject(FdUiDialogService);
    public readonly recipe = input.required<Recipe>();

    protected readonly language = injectCurrentLanguage();
    protected readonly currentStepIndex = signal(0);
    protected readonly steps = computed(() => [...this.recipe().steps].sort((a, b) => a.stepNumber - b.stepNumber));
    protected readonly stepCount = computed(() => this.steps().length);
    protected readonly currentStep = computed(() => {
        const step = this.steps().at(this.currentStepIndex());
        return step === undefined ? null : { ...step, photos: this.getStepPhotos(step) };
    });
    protected readonly progressPercent = computed(() => {
        const count = this.stepCount();
        return count > 0 ? ((this.currentStepIndex() + 1) / count) * PERCENT_SCALE : 0;
    });
    protected readonly ingredients = computed<CookModeIngredientView[]>(() =>
        this.buildIngredientViews(this.currentStep()?.ingredients ?? []),
    );
    protected readonly canGoBack = computed(() => this.currentStepIndex() > 0);
    protected readonly canGoNext = computed(() => this.currentStepIndex() < this.stepCount() - 1);
    protected readonly isDone = computed(() => this.stepCount() > 0 && !this.canGoNext());

    protected previewPhoto(index: number, photos: readonly string[]): void {
        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            data: { collageImages: photos.map(url => ({ url })), initialIndex: index },
        });
    }

    protected previousStep(): void {
        if (this.canGoBack()) {
            this.currentStepIndex.update(index => index - 1);
        }
    }

    protected nextStep(): void {
        if (this.canGoNext()) {
            this.currentStepIndex.update(index => index + 1);
        }
    }

    private getStepPhotos(step: RecipeStep): string[] {
        if (step.images !== undefined && step.images.length > 0) {
            return step.images.map(image => image.imageUrl);
        }
        return typeof step.imageUrl === 'string' && step.imageUrl.length > 0 ? [step.imageUrl] : [];
    }

    private buildIngredientViews(ingredients: readonly RecipeIngredient[]): CookModeIngredientView[] {
        return ingredients.map(ingredient => ({
            name: capitalizeFirstLetter(ingredient.textName ?? ingredient.productName ?? ingredient.nestedRecipeName),
            amount: typeof ingredient.textName === 'string' ? (ingredient.amountText ?? '') : ingredient.amount,
            unitKey: resolveIngredientUnitKey(ingredient) ?? '',
        }));
    }
}
