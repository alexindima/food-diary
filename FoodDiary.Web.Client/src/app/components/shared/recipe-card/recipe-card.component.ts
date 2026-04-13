import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { NutrientBadgesComponent } from '../nutrient-badges/nutrient-badges.component';
import { MediaCardComponent } from '../media-card/media-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';

export interface RecipeCardStep {
    ingredients?: Array<unknown> | null;
}

export interface RecipeCardItem {
    name: string;
    imageUrl?: string | null;
    isOwnedByCurrentUser: boolean;
    prepTime?: number | null;
    cookTime?: number | null;
    totalProteins?: number | null;
    totalFats?: number | null;
    totalCarbs?: number | null;
    totalFiber?: number | null;
    totalAlcohol?: number | null;
    totalCalories?: number | null;
    steps?: RecipeCardStep[] | null;
}

@Component({
    selector: 'fd-recipe-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiIconModule, FdUiButtonComponent, NutrientBadgesComponent, MediaCardComponent],
    templateUrl: './recipe-card.component.html',
    styleUrl: './recipe-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);

    public readonly recipe = input.required<RecipeCardItem>();
    public readonly imageUrl = input<string>();
    public readonly open = output<void>();
    public readonly addToMeal = output<void>();

    public handleOpen(): void {
        this.open.emit();
    }

    public handleAdd(event: Event): void {
        event.stopPropagation();
        this.addToMeal.emit();
    }

    public hasPreviewImage(): boolean {
        return Boolean(this.imageUrl()?.trim());
    }

    public handlePreview(event: Event): void {
        event.stopPropagation();

        const imageUrl = this.imageUrl()?.trim();
        if (!imageUrl) {
            return;
        }

        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'min(calc(100vw - 3rem), 1200px)',
            maxWidth: '1200px',
            data: {
                imageUrl,
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', { name: this.recipe().name }),
                title: this.recipe().name,
            },
        });
    }

    public getTotalTime(): number | null {
        const r = this.recipe();
        const prep = r?.prepTime ?? 0;
        const cook = r?.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    }

    public getIngredientCount(): number {
        const r = this.recipe();
        if (!r?.steps?.length) {
            return 0;
        }

        return r.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    }
}
