import type { Routes } from '@angular/router';

export const authRoutes: Routes = [
    {
        path: 'verify-pending',
        loadComponent: async () =>
            import('./pages/email-verification-pending/email-verification-pending').then(m => m.EmailVerificationPendingComponent),
        data: { seo: { titleKey: 'SEO.VERIFY_PENDING', noIndex: true } },
    },
    {
        path: 'verify-email',
        loadComponent: async () => import('./pages/email-verification/email-verification').then(m => m.EmailVerificationComponent),
        data: { seo: { titleKey: 'SEO.VERIFY_EMAIL', noIndex: true } },
    },
    {
        path: 'reset-password',
        loadComponent: async () => import('./pages/password-reset/password-reset').then(m => m.PasswordResetComponent),
        data: { seo: { titleKey: 'SEO.RESET_PASSWORD', descriptionKey: 'SEO.RESET_PASSWORD_DESCRIPTION' } },
    },
];
