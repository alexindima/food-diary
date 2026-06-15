import { ChangeDetectionStrategy, Component, DestroyRef, effect, type ElementRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { DashboardSummaryCardComponent } from '../../../../components/shared/dashboard-summary-card/dashboard-summary-card';
import { MealsPreviewComponent } from '../../../../components/shared/meals-preview/meals-preview';
import type { MealPreviewEntry } from '../../../../components/shared/meals-preview/meals-preview-lib/meals-preview.types';
import { ProductCardComponent } from '../../../../components/shared/product-card/product-card';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card';
import { AuthService } from '../../../../services/auth.service';
import { QuickConsumptionDrawerComponent } from '../../../meals/components/quick-consumption-drawer/quick-consumption-drawer';
import { type QuickMealItem, QuickMealService } from '../../../meals/lib/quick/quick-meal.service';
import type { Product } from '../../../products/models/product.data';
import type { Recipe } from '../../../recipes/models/recipe.data';
import type { PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { PublicAuthNavigationService } from '../../lib/public-auth-navigation.service';
import { buildLandingPreviewContent, type LandingPreviewContent } from './landing-preview-tour-data.mapper';

@Component({
    selector: 'fd-landing-preview-tour',
    imports: [
        TranslatePipe,
        DashboardSummaryCardComponent,
        MealsPreviewComponent,
        ProductCardComponent,
        RecipeCardComponent,
        QuickConsumptionDrawerComponent,
    ],
    templateUrl: './landing-preview-tour.html',
    styleUrls: ['./landing-preview-tour.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPreviewTourComponent {
    private readonly authService = inject(AuthService);
    private readonly authNavigationService = inject(PublicAuthNavigationService);
    private readonly quickConsumptionService = inject(QuickMealService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly draftBlock = viewChild<ElementRef<HTMLElement>>('draftBlock');

    protected isAuthenticated = this.authService.isAuthenticated;
    protected heroSummaryCard: LandingPreviewContent['heroSummaryCard'] = buildLandingPreviewContent(key => key).heroSummaryCard;
    protected guestMealEntries: MealPreviewEntry[] = [];
    protected previewProducts: Product[] = [];
    protected previewRecipes: Recipe[] = [];
    protected previewQuickItems: QuickMealItem[] = [];

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

    protected addPreviewProduct(product: Product): void {
        this.quickConsumptionService.addProduct(product);
        this.scrollDraftIntoView();
    }

    protected addPreviewRecipe(recipe: Recipe): void {
        this.quickConsumptionService.addRecipe(recipe);
        this.scrollDraftIntoView();
    }

    protected async navigateToAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authNavigationService.navigateAsync(mode);
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
