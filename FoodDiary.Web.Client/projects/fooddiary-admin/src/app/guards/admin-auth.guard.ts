import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AdminAuthService } from '../services/admin-auth.service';

export const adminAuthGuard: CanActivateFn = async (_route, state) => {
  const authService = inject(AdminAuthService);
  const router = inject(Router);

  await authService.applySsoFromQuery();
  authService.refreshTokenState();

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/unauthorized'], {
      queryParams: { reason: 'unauthenticated', returnUrl: state.url },
    });
  }

  if (!authService.isAdmin()) {
    return router.createUrlTree(['/unauthorized'], {
      queryParams: { reason: 'forbidden', returnUrl: state.url },
    });
  }

  return true;
};
