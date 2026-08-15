import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { GoogleIdentityService } from '../../../../../shared/auth/google-identity.service';
import { UserManageSecurityCardComponent } from './user-manage-security-card';

describe('UserManageSecurityCardComponent', () => {
    const originalClientId = environment.googleClientId;
    const googleIdentityService = {
        initializeAsync: vi.fn(),
        renderButton: vi.fn(),
    };

    beforeEach(() => {
        environment.googleClientId = originalClientId;
        googleIdentityService.initializeAsync.mockReset();
        googleIdentityService.renderButton.mockReset();
    });

    it('shows the linked Google account without initializing another sign-in button', async () => {
        const fixture = await createFixtureAsync(true);

        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.GOOGLE_CONNECTED');
        expect(fixture.componentInstance.email()).toBe('alex@example.com');
        expect(googleIdentityService.initializeAsync).not.toHaveBeenCalled();
    });

    it('renders a Google link button and emits the returned credential', async () => {
        environment.googleClientId = 'client-id';
        let callback: ((credential: string) => void) | undefined;
        googleIdentityService.initializeAsync.mockImplementation((options: { callback: (credential: string) => void }) => {
            callback = options.callback;
        });
        const fixture = await createFixtureAsync(false);
        const emitted = vi.fn();
        fixture.componentInstance.googleCredential.subscribe(emitted);

        await fixture.whenStable();
        callback?.('credential');

        expect(googleIdentityService.renderButton).toHaveBeenCalled();
        expect(emitted).toHaveBeenCalledWith('credential');
    });

    it('emits a password change request from the security section', async () => {
        const fixture = await createFixtureAsync(true);
        const emitted = vi.fn();
        fixture.componentInstance.passwordChange.subscribe(emitted);

        const button = (fixture.nativeElement as HTMLElement).querySelector('button');
        button?.click();

        expect(emitted).toHaveBeenCalledOnce();
    });

    async function createFixtureAsync(hasGoogleIdentity: boolean): Promise<ComponentFixture<UserManageSecurityCardComponent>> {
        await TestBed.configureTestingModule({
            imports: [UserManageSecurityCardComponent],
            providers: [provideTranslateTesting(), { provide: GoogleIdentityService, useValue: googleIdentityService }],
        }).compileComponents();

        const fixture = TestBed.createComponent(UserManageSecurityCardComponent);
        fixture.componentRef.setInput('email', 'alex@example.com');
        fixture.componentRef.setInput('hasGoogleIdentity', hasGoogleIdentity);
        fixture.componentRef.setInput('isLinkingGoogle', false);
        fixture.componentRef.setInput('passwordActionState', {
            buttonLabelKey: 'USER_MANAGE.CHANGE_PASSWORD',
            descriptionKey: 'USER_MANAGE.CHANGE_PASSWORD_DESCRIPTION',
        });
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        return fixture;
    }
});
