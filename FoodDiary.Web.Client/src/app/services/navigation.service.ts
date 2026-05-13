import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';

import type { AuthMode } from '../features/auth/models/auth.data';

@Injectable({ providedIn: 'root' })
export class NavigationService {
    private readonly router = inject(Router);

    public async navigateToHomeAsync(): Promise<void> {
        await this.router.navigate(['/dashboard']);
    }

    public async navigateToLandingAsync(): Promise<void> {
        await this.router.navigate(['/']);
    }

    public async navigateToAuthAsync(mode: AuthMode, returnUrl?: string): Promise<void> {
        const queryParams = returnUrl !== undefined && returnUrl.length > 0 ? { returnUrl } : {};
        await this.router.navigate(['/auth', mode], { queryParams });
    }

    public async navigateToEmailVerificationPendingAsync(options?: { autoResend?: boolean }): Promise<void> {
        const queryParams = options?.autoResend === true ? { autoResend: 'true' } : {};
        await this.router.navigate(['/verify-pending'], { queryParams });
    }

    public async navigateToReturnUrlAsync(returnUrl: string | null): Promise<void> {
        await this.router.navigateByUrl(returnUrl ?? '/dashboard');
    }

    public async navigateToProductListAsync(): Promise<void> {
        await this.router.navigate(['/products']);
    }

    public async navigateToProductAddAsync(extras?: { state?: Record<string, unknown> }): Promise<void> {
        await this.router.navigate(['/products/add'], extras);
    }

    public async navigateToProductEditAsync(id: string): Promise<void> {
        await this.router.navigate([`/products/${id}/edit`]);
    }

    public async navigateToConsumptionListAsync(): Promise<void> {
        await this.router.navigate(['/meals']);
    }

    public async navigateToConsumptionAddAsync(mealType?: string, extras?: { state?: Record<string, unknown> }): Promise<boolean> {
        const navigationExtras =
            mealType !== undefined && mealType.length > 0
                ? {
                      state: { mealType, ...(extras?.state ?? {}) },
                      queryParams: { mealType },
                  }
                : { state: extras?.state };
        return this.router.navigate(['/meals/add'], navigationExtras);
    }

    public async navigateToConsumptionEditAsync(id: string): Promise<void> {
        await this.router.navigate([`/meals/${id}/edit`]);
    }

    public async navigateToRecipeListAsync(): Promise<void> {
        await this.router.navigate(['/recipes']);
    }

    public async navigateToRecipeAddAsync(): Promise<void> {
        await this.router.navigate(['/recipes/add']);
    }

    public async navigateToRecipeEditAsync(id: string): Promise<void> {
        await this.router.navigate([`/recipes/${id}/edit`]);
    }

    public async navigateToShoppingListAsync(): Promise<void> {
        await this.router.navigate(['/shopping-lists']);
    }

    public async navigateToFastingAsync(): Promise<void> {
        await this.router.navigate(['/fasting']);
    }

    public async navigateToStatisticsAsync(): Promise<void> {
        await this.router.navigate(['/statistics']);
    }

    public async navigateToProfileAsync(): Promise<void> {
        await this.router.navigate(['/profile']);
    }

    public async navigateToDietologistAsync(): Promise<void> {
        await this.router.navigate(['/dietologist']);
    }

    public async navigateToPremiumAccessAsync(): Promise<void> {
        await this.router.navigate(['/premium']);
    }

    public async navigateToWeightHistoryAsync(): Promise<void> {
        await this.router.navigate(['/weight-history']);
    }

    public async navigateToWaistHistoryAsync(): Promise<void> {
        await this.router.navigate(['/waist-history']);
    }

    public async navigateToGoalsAsync(): Promise<void> {
        await this.router.navigate(['/goals']);
    }

    public async navigateToCycleTrackingAsync(): Promise<void> {
        await this.router.navigate(['/cycle-tracking']);
    }

    public async navigateToExploreAsync(): Promise<void> {
        await this.router.navigate(['/explore']);
    }
}
