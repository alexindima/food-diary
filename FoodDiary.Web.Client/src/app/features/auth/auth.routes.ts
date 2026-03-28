import { Routes } from '@angular/router';
import { MainComponent } from '../../components/main/main.component';
import { loggedInGuard } from '../../guards/logged-in.guard';
import { EmailVerificationPendingComponent } from './pages/email-verification-pending/email-verification-pending.component';
import { EmailVerificationComponent } from './pages/email-verification/email-verification.component';
import { PasswordResetComponent } from './pages/password-reset/password-reset.component';

export const authRoutes: Routes = [
    {
        path: 'auth',
        component: MainComponent,
        data: { openAuth: true },
        canActivate: [loggedInGuard],
    },
    {
        path: 'auth/:mode',
        component: MainComponent,
        data: { openAuth: true },
        canActivate: [loggedInGuard],
    },
    {
        path: 'verify-pending',
        component: EmailVerificationPendingComponent,
    },
    {
        path: 'verify-email',
        component: EmailVerificationComponent,
    },
    {
        path: 'reset-password',
        component: PasswordResetComponent,
    },
];
