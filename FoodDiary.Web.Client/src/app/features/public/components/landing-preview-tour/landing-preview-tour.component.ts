import { ChangeDetectionStrategy, Component, DestroyRef, effect, type ElementRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { DashboardSummaryCardComponent } from '../../../../components/shared/dashboard-summary-card/dashboard-summary-card.component';
import { MealsPreviewComponent } from '../../../../components/shared/meals-preview/meals-preview.component';
import type { MealPreviewEntry } from '../../../../components/shared/meals-preview/meals-preview-lib/meals-preview.types';
import { ProductCardComponent } from '../../../../components/shared/product-card/product-card.component';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card.component';
import { AuthService } from '../../../../services/auth.service';
import { QuickConsumptionDrawerComponent } from '../../../meals/components/quick-consumption-drawer/quick-consumption-drawer.component';
import { type QuickMealItem, QuickMealService } from '../../../meals/lib/quick/quick-meal.service';
import type { Product } from '../../../products/models/product.data';
import type { Recipe } from '../../../recipes/models/recipe.data';
import { PublicAuthDialogService, type PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { buildLandingPreviewContent, type LandingPreviewContent } from './landing-preview-tour-data.mapper';

@Component({
    selector: 'fd-landing-preview-tour',
    imports: [
        TranslateModule,
        DashboardSummaryCardComponent,
        MealsPreviewComponent,
        ProductCardComponent,
        RecipeCardComponent,
        QuickConsumptionDrawerComponent,
    ],
    templateUrl: './landing-preview-tour.component.html',
    styleUrls: ['./landing-preview-tour.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPreviewTourComponent {
    private readonly authService = inject(AuthService);
    private readonly authDialogService = inject(PublicAuthDialogService);
    private readonly quickConsumptionService = inject(QuickMealService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly draftBlock = viewChild<ElementRef<HTMLElement>>('draftBlock');

    public isAuthenticated = this.authService.isAuthenticated;
    public heroSummaryCard: LandingPreviewContent['heroSummaryCard'] = buildLandingPreviewContent(key => key).heroSummaryCard;
    public guestMealEntries: MealPreviewEntry[] = [];
    public previewProducts: Product[] = [];
    public previewRecipes: Recipe[] = [];
    public previewQuickItems: QuickMealItem[] = [];

    public constructor() {
        effect(() => {
            if (this.isAuthenticated()) {
                this.quickConsumptionService.exitPreview();
            }
        });

        this.refreshPreviewContent();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.refreshPreviewContent();
        });
    }

    public addPreviewProduct(product: Product): void {
        this.quickConsumptionService.addProduct(product);
        this.scrollDraftIntoView();
    }

    public addPreviewRecipe(recipe: Recipe): void {
        this.quickConsumptionService.addRecipe(recipe);
        this.scrollDraftIntoView();
    }

    public async openAuthDialogAsync(mode: PublicAuthMode): Promise<void> {
        await this.authDialogService.openAsync({ mode });
    }

    private refreshPreviewContent(): void {
        const content = buildLandingPreviewContent(key => this.translateService.instant(key));
        this.heroSummaryCard = content.heroSummaryCard;
        this.previewProducts = content.previewProducts;
        this.previewRecipes = content.previewRecipes;
        this.previewQuickItems = content.previewQuickItems;
        this.guestMealEntries = content.guestMealEntries;

        if (this.isAuthenticated()) {
            return;
        }

        this.quickConsumptionService.setPreviewItems(this.previewQuickItems);
    }

    private scrollDraftIntoView(): void {
        const draftBlock = this.draftBlock()?.nativeElement;
        if (draftBlock === undefined) {
            return;
        }

        if (typeof window === 'undefined') {
            return;
        }

        const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const scroll = (): void => {
            draftBlock.scrollIntoView({
                behavior: prefersReducedMotion ? 'auto' : 'smooth',
                block: 'center',
                inline: 'nearest',
            });
        };

        window.requestAnimationFrame(scroll);
    }
}
