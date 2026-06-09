import type { Routes } from '@angular/router';

export const authRoutes: Routes = [
    {
        path: 'mobile',
        redirectTo: 'mobile/login',
        pathMatch: 'full',
    },
    {
        path: 'mobile/login',
        loadComponent: async () => import('./pages/mobile-login/mobile-login').then(m => m.MobileLoginComponent),
        data: { seo: { titleKey: 'SEO.AUTH_LOGIN', descriptionKey: 'SEO.AUTH_DESCRIPTION', noIndex: true } },
    },
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
