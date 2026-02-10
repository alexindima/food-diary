import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthMode } from '../types/auth.data';

@Injectable({
    providedIn: 'root',
})
export class NavigationService {
    private readonly router = inject(Router);

    public async navigateToHome(): Promise<void> {
        await this.router.navigate(['/']);
    }

    public async navigateToAuth(mode: AuthMode, returnUrl?: string): Promise<void> {
        const queryParams = returnUrl ? { returnUrl } : {};
        await this.router.navigate(['/auth', mode], { queryParams });
    }

    public async navigateToEmailVerificationPending(): Promise<void> {
        await this.router.navigate(['/verify-pending']);
    }

    public async navigateToReturnUrl(returnUrl: string | null): Promise<void> {
        await this.router.navigateByUrl(returnUrl || '/');
    }

    public async navigateToProductList(): Promise<void> {
        await this.router.navigate(['/products']);
    }

    public async navigateToProductAdd(): Promise<void> {
        await this.router.navigate(['/products/add']);
    }

    public async navigateToProductEdit(id: string): Promise<void> {
        await this.router.navigate([`/products/${id}/edit`]);
    }

    public async navigateToConsumptionList(): Promise<void> {
        await this.router.navigate(['/meals']);
    }

    public async navigateToConsumptionAdd(mealType?: string, extras?: { state?: Record<string, unknown> }): Promise<void> {
        const navigationExtras = mealType
            ? {
                  state: { mealType, ...(extras?.state ?? {}) },
                  queryParams: { mealType },
              }
            : { state: extras?.state };
        await this.router.navigate(['/meals/add'], navigationExtras);
    }

    public async navigateToConsumptionEdit(id: string): Promise<void> {
        await this.router.navigate([`/meals/${id}/edit`]);
    }

    public async navigateToRecipeList(): Promise<void> {
        await this.router.navigate(['/recipes']);
    }

    public async navigateToRecipeAdd(): Promise<void> {
        await this.router.navigate(['/recipes/add']);
    }

    public async navigateToRecipeEdit(id: string): Promise<void> {
        await this.router.navigate([`/recipes/${id}/edit`]);
    }

    public async navigateToShoppingList(): Promise<void> {
        await this.router.navigate(['/shopping-lists']);
    }

    public async navigateToStatistics(): Promise<void> {
        await this.router.navigate(['/statistics']);
    }

    public async navigateToProfile(): Promise<void> {
        await this.router.navigate(['/profile']);
    }

    public async navigateToPremiumAccess(): Promise<void> {
        await this.router.navigate(['/premium']);
    }

    public async navigateToWeightHistory(): Promise<void> {
        await this.router.navigate(['/weight-history']);
    }

    public async navigateToWaistHistory(): Promise<void> {
        await this.router.navigate(['/waist-history']);
    }

    public async navigateToGoals(): Promise<void> {
        await this.router.navigate(['/goals']);
    }

    public async navigateToCycleTracking(): Promise<void> {
        await this.router.navigate(['/cycle-tracking']);
    }
}
