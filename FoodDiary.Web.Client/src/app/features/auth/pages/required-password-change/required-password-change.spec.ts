import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { RequiredPasswordChangeComponent } from './required-password-change';

describe('RequiredPasswordChangeComponent', () => {
    it('renders the required password-change route with its dependencies', () => {
        const fixture = TestBed.configureTestingModule({
            imports: [RequiredPasswordChangeComponent],
            providers: [
                { provide: UserFacade, useValue: { changePassword: vi.fn().mockReturnValue(of(true)) } },
                {
                    provide: AuthService,
                    useValue: { completeRequiredPasswordChange: vi.fn(), onLogoutAsync: vi.fn().mockResolvedValue(void 0) },
                },
                { provide: NavigationService, useValue: { navigateToHomeAsync: vi.fn().mockResolvedValue(void 0) } },
                { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            ],
        })
            .overrideComponent(RequiredPasswordChangeComponent, { set: { template: '' } })
            .createComponent(RequiredPasswordChangeComponent);

        fixture.detectChanges();

        expect(fixture.componentInstance).toBeTruthy();
    });
});
