import type { Routes } from '@angular/router';

import { loggedInGuard } from '../../guards/logged-in.guard';

export const authRoutes: Routes = [
    {
        path: 'auth',
        loadComponent: async () => import('../public/pages/landing/main.component').then(m => m.MainComponent),
        data: { openAuth: true, seo: { titleKey: 'SEO.AUTH_LOGIN', descriptionKey: 'SEO.AUTH_DESCRIPTION' } },
        canActivate: [loggedInGuard],
    },
    {
        path: 'auth/:mode',
        loadComponent: async () => import('../public/pages/landing/main.component').then(m => m.MainComponent),
        data: { openAuth: true, seo: { titleKey: 'SEO.AUTH_LOGIN', descriptionKey: 'SEO.AUTH_DESCRIPTION' } },
        canActivate: [loggedInGuard],
    },
    {
        path: 'verify-pending',
        loadComponent: async () =>
            import('./pages/email-verification-pending/email-verification-pending.component').then(
                m => m.EmailVerificationPendingComponent,
            ),
        data: { seo: { titleKey: 'SEO.VERIFY_PENDING', noIndex: true } },
    },
    {
        path: 'verify-email',
        loadComponent: async () =>
            import('./pages/email-verification/email-verification.component').then(m => m.EmailVerificationComponent),
        data: { seo: { titleKey: 'SEO.VERIFY_EMAIL', noIndex: true } },
    },
    {
        path: 'reset-password',
        loadComponent: async () => import('./pages/password-reset/password-reset.component').then(m => m.PasswordResetComponent),
        data: { seo: { titleKey: 'SEO.RESET_PASSWORD', descriptionKey: 'SEO.RESET_PASSWORD_DESCRIPTION' } },
    },
];
