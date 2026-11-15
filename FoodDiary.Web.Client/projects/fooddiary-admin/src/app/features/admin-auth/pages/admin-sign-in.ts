import { DOCUMENT } from '@angular/common';
import { afterNextRender, ChangeDetectionStrategy, Component, type ElementRef, inject, signal, viewChild } from '@angular/core';
import { email, form, FormField, FormRoot, required } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { environment } from '../../../../environments/environment';
import { adminSignInAccount, adminSignInAccountFragment, adminSignInReturnUrl } from '../contracts/admin-sign-in-navigation';
import { AdminAuthService, adminSignInErrorKey } from '../lib/admin-auth.service';
import { AdminGoogleSignInService } from '../lib/admin-google-sign-in.service';

@Component({
    selector: 'fd-admin-sign-in',
    imports: [FormField, FormRoot, TranslatePipe, FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent],
    templateUrl: './admin-sign-in.html',
    styleUrl: './admin-sign-in.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminSignInComponent {
    private readonly document = inject(DOCUMENT);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly auth = inject(AdminAuthService);
    private readonly google = inject(AdminGoogleSignInService);
    private readonly translate = inject(TranslateService);
    private readonly googleButton = viewChild<ElementRef<HTMLDivElement>>('googleButton');
    private readonly expectedUserId = adminSignInAccount(this.route.snapshot.fragment);
    private readonly returnUrl = adminSignInReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'), this.document.location.origin);
    private readonly credentials = signal({ email: '', password: '' });

    protected readonly confirm = this.route.snapshot.queryParamMap.get('confirm') === '1';
    protected readonly googleConfigured = environment.googleClientId.length > 0;
    protected readonly googleUnavailable = signal(false);
    protected readonly submitting = signal(false);
    protected readonly error = signal<string | null>(null);
    private readonly submitPasswordAsync = async (): Promise<void> => {
        const { email: address, password } = this.credentials();
        await this.signInAsync(async () => this.auth.signInWithPasswordAsync(address.trim(), password, this.expectedUserId));
    };
    protected readonly loginForm = form(
        this.credentials,
        fields => {
            required(fields.email);
            email(fields.email);
            required(fields.password);
        },
        { submission: { action: this.submitPasswordAsync } },
    );

    public constructor() {
        afterNextRender(() => {
            if (this.confirm) {
                void this.renderGoogleAsync();
            } else {
                void this.trySsoAsync();
            }
        });
    }

    private async trySsoAsync(): Promise<void> {
        if (this.route.snapshot.queryParamMap.get('confirm') === '0') {
            this.redirectToDiary();
            return;
        }
        await this.auth.restoreSessionAsync();
        if (this.auth.sessionUnavailable()) {
            await this.router.navigate(['/unauthorized'], {
                queryParams: { reason: 'unavailable', returnUrl: this.returnUrl },
                fragment: adminSignInAccountFragment(this.expectedUserId),
                replaceUrl: true,
            });
            return;
        }
        if (this.auth.isAdmin() && (this.expectedUserId === null || this.auth.getUserId() === this.expectedUserId)) {
            await this.router.navigateByUrl(this.returnUrl, { replaceUrl: true });
            return;
        }
        this.redirectToDiary();
    }

    private redirectToDiary(): void {
        const url = new URL('/auth/confirm-admin', environment.mainAppUrl);
        url.searchParams.set('adminReturnUrl', this.returnUrl);
        url.hash = adminSignInAccountFragment(this.expectedUserId) ?? '';
        this.document.location.replace(url.href);
    }

    private async renderGoogleAsync(): Promise<void> {
        const target = this.googleButton()?.nativeElement;
        if (target === undefined || !this.googleConfigured) {
            return;
        }
        try {
            await this.google.renderAsync(target, environment.googleClientId, this.translate.getCurrentLang() ?? 'en', credential => {
                void this.signInAsync(async () => this.auth.signInWithGoogleAsync(credential, this.expectedUserId));
            });
        } catch {
            this.googleUnavailable.set(true);
        }
    }

    private async signInAsync(request: () => Promise<boolean>): Promise<void> {
        if (this.submitting()) {
            return;
        }
        this.submitting.set(true);
        this.error.set(null);
        try {
            if (await request()) {
                await this.router.navigateByUrl(this.returnUrl, { replaceUrl: true });
            } else {
                this.error.set('ADMIN_AUTH.SIGN_IN_ERROR');
            }
        } catch (error: unknown) {
            this.error.set(adminSignInErrorKey(error));
        } finally {
            this.submitting.set(false);
        }
    }
}
