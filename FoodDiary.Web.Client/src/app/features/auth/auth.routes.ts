import { Routes } from '@angular/router';
import { MainComponent } from '../public/pages/landing/main.component';
import { loggedInGuard } from '../../guards/logged-in.guard';
import { EmailVerificationPendingComponent } from './pages/email-verification-pending/email-verification-pending.component';
import { EmailVerificationComponent } from './pages/email-verification/email-verification.component';
import { PasswordResetComponent } from './pages/password-reset/password-reset.component';

export const authRoutes: Routes = [
    {
        path: 'auth',
        component: MainComponent,
        data: { openAuth: true, seo: { titleKey: 'SEO.AUTH_LOGIN', descriptionKey: 'SEO.AUTH_DESCRIPTION' } },
        canActivate: [loggedInGuard],
    },
    {
        path: 'auth/:mode',
        component: MainComponent,
        data: { openAuth: true, seo: { titleKey: 'SEO.AUTH_LOGIN', descriptionKey: 'SEO.AUTH_DESCRIPTION' } },
        canActivate: [loggedInGuard],
    },
    {
        path: 'verify-pending',
        component: EmailVerificationPendingComponent,
        data: { seo: { titleKey: 'SEO.VERIFY_PENDING', noIndex: true } },
    },
    {
        path: 'verify-email',
        component: EmailVerificationComponent,
        data: { seo: { titleKey: 'SEO.VERIFY_EMAIL', noIndex: true } },
    },
    {
        path: 'reset-password',
        component: PasswordResetComponent,
        data: { seo: { titleKey: 'SEO.RESET_PASSWORD', descriptionKey: 'SEO.RESET_PASSWORD_DESCRIPTION' } },
    },
];
