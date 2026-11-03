import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { PrivacyConsentFacade } from './privacy-consent.facade';

export const healthConsentGuard: CanActivateFn = async () => {
    const auth = inject(AuthService);
    const privacy = inject(PrivacyConsentFacade);
    const router = inject(Router);
    if (!auth.isAuthenticated()) {
        return true;
    }
    return (await privacy.ensureHealthConsentAsync()) ? true : router.createUrlTree(['/privacy-settings']);
};
