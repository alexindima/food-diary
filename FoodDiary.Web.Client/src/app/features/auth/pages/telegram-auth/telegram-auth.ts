import { afterNextRender, ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { form, FormField, required } from '@angular/forms/signals';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { TelegramBackupEmailFlowService } from '../../../../shared/auth/telegram-backup-email-flow.service';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { AuthComponent } from '../../components/auth/auth';
import { TelegramAuthFacade } from '../../lib/telegram-auth.facade';
import { telegramTimeZoneOptions } from '../../lib/telegram-time-zone-options';

@Component({
    selector: 'fd-telegram-auth',
    imports: [TranslatePipe, FormField, FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent, FdUiSelectComponent, AuthComponent],
    providers: [TelegramAuthFacade],
    templateUrl: './telegram-auth.html',
    styleUrl: './telegram-auth.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TelegramAuthComponent {
    protected readonly facade = inject(TelegramAuthFacade);
    protected readonly auth = inject(AuthService);
    private readonly users = inject(UserFacade);
    private readonly navigation = inject(NavigationService);
    private readonly locale = inject(LocalizationService);
    private readonly browser = inject(BrowserWindowService);
    private readonly route = inject(ActivatedRoute);
    private readonly backupEmailFlow = inject(TelegramBackupEmailFlowService);
    protected readonly ready = signal(false);
    protected readonly showExistingLogin = signal(false);
    protected readonly registrationModel = signal({ timeZoneId: 'UTC' });
    protected readonly timeZoneSearch = signal('');
    protected readonly timeZoneOptions = signal(telegramTimeZoneOptions('UTC', new Date()));
    protected readonly filteredTimeZoneOptions = computed(() => {
        const query = this.timeZoneSearch().trim().toLowerCase();
        const selected = this.registrationModel().timeZoneId;
        return this.timeZoneOptions().filter(option => option.value === selected || option.label.toLowerCase().includes(query));
    });
    protected readonly registrationForm = form(this.registrationModel, fields => {
        required(fields.timeZoneId);
    });
    protected readonly targetAccount = computed(() => {
        const user = this.users.user();
        if (user === null) {
            return null;
        }
        const fullName = [user.firstName, user.lastName]
            .filter(part => typeof part === 'string' && part.trim().length > 0)
            .join(' ')
            .trim();
        return fullName.length > 0 ? fullName : (user.email ?? user.username ?? null);
    });
    protected readonly canLink = computed(() => this.auth.isAuthenticated() && this.users.user() !== null && !this.facade.busy());

    public constructor() {
        effect(() => {
            if (this.auth.isAuthenticated()) {
                this.showExistingLogin.set(false);
                void firstValueFrom(this.users.getInfo());
            }
        });
        afterNextRender(() => {
            void this.initializeAsync();
        });
    }

    protected begin(): void {
        void this.facade.beginAsync(this.auth.isAuthenticated());
    }

    protected openOtherSignIn(): void {
        void this.navigation.navigateToAuthAsync('login');
    }

    protected complete(action: 'login' | 'register' | 'link'): void {
        void this.completeAsync(action);
    }

    protected chooseExisting(): void {
        this.showExistingLogin.set(true);
    }

    protected backToChoice(): void {
        this.showExistingLogin.set(false);
    }

    protected restart(): void {
        this.showExistingLogin.set(false);
        this.facade.clearIntent();
        this.facade.errorKey.set(null);
    }

    private async initializeAsync(): Promise<void> {
        const timeZoneId = new Intl.DateTimeFormat().resolvedOptions().timeZone;
        this.registrationModel.set({ timeZoneId: timeZoneId.length > 0 ? timeZoneId : 'UTC' });
        this.timeZoneOptions.set(telegramTimeZoneOptions(this.registrationModel().timeZoneId, new Date()));
        const code = this.route.snapshot.queryParamMap.get('code');
        const state = this.route.snapshot.queryParamMap.get('state');
        const denied = this.route.snapshot.queryParamMap.has('error');
        if (code !== null || state !== null || denied) {
            this.browser.replaceCurrentUrl('/auth/telegram');
            if (await this.backupEmailFlow.handleCallbackAsync(code, state, denied)) {
                await this.navigation.navigateToProfileAsync();
                return;
            }
            if (code !== null && state !== null && !denied) {
                await this.facade.exchangeAsync(code, state);
            } else {
                this.facade.errorKey.set('AUTH.TELEGRAM.CANCELLED');
            }
        } else {
            this.facade.restoreIntent();
        }
        await this.facade.loadConfigurationAsync();
        this.ready.set(true);
    }

    private async completeAsync(action: 'login' | 'register' | 'link'): Promise<void> {
        if (action === 'register' && this.registrationForm().invalid()) {
            this.registrationForm().markAsTouched();
            return;
        }
        if (!(await this.facade.completeAsync(action, this.locale.getCurrentLanguage(), this.registrationModel().timeZoneId))) {
            return;
        }
        if (this.auth.requiresEmailVerification()) {
            await this.navigation.navigateToEmailVerificationPendingAsync();
        } else if (this.auth.mustChangePassword()) {
            await this.navigation.navigateToRequiredPasswordChangeAsync();
        } else {
            await this.navigation.navigateToHomeAsync();
        }
    }
}
