import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';

import type { PublicAuthMode } from './public-auth-dialog.service';

@Injectable({ providedIn: 'root' })
export class PublicAuthNavigationService {
    private readonly router = inject(Router);

    public async navigateAsync(mode: PublicAuthMode): Promise<boolean> {
        return this.router.navigate(['/'], { queryParams: { auth: mode } });
    }
}
