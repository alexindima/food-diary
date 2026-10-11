import { inject, Service } from '@angular/core';
import { Router } from '@angular/router';

import { localizedPublicPath, publicSiteLocale } from '../../../config/public-site-paths';
import type { PublicAuthMode } from './public-auth-dialog.service';

@Service()
export class PublicAuthNavigationService {
    private readonly router = inject(Router);

    public async navigateAsync(mode: PublicAuthMode): Promise<boolean> {
        return this.router.navigate([localizedPublicPath('/', publicSiteLocale(this.router.url))], { queryParams: { auth: mode } });
    }
}
