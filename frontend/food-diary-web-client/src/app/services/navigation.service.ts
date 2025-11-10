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
        await this.router.navigate(['/consumptions']);
    }

    public async navigateToConsumptionAdd(): Promise<void> {
        await this.router.navigate(['/consumptions/add']);
    }

    public async navigateToConsumptionEdit(id: number): Promise<void> {
        await this.router.navigate([`/consumptions/${id}/edit`]);
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

    public async navigateToStatistics(): Promise<void> {
        await this.router.navigate(['/statistics']);
    }

    public async navigateToProfile(): Promise<void> {
        await this.router.navigate(['/profile']);
    }
}
