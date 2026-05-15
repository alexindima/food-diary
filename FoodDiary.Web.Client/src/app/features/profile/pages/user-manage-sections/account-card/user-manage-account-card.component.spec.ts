import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { FrontendLoggerService } from '../../../../../services/frontend-logger.service';
import { ImageUploadService } from '../../../../../shared/api/image-upload.service';
import { Gender } from '../../../../../shared/models/user.data';
import { createUserManageForm } from '../../user-manage/user-manage-form.mapper';
import { UserManageAccountCardComponent } from './user-manage-account-card.component';

describe('UserManageAccountCardComponent', () => {
    it('renders with account form inputs and emits password change', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance;
        const passwordChange = vi.fn();
        component.passwordChange.subscribe(passwordChange);

        component.passwordChange.emit();

        expect(passwordChange).toHaveBeenCalledTimes(1);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.ACCOUNT_SECTION');
    });
});

async function createComponentAsync(): Promise<ComponentFixture<UserManageAccountCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageAccountCardComponent, TranslateModule.forRoot()],
        providers: [
            {
                provide: ImageUploadService,
                useValue: { uploadImage: vi.fn().mockReturnValue(of(null)), deleteAsset: vi.fn().mockReturnValue(of(undefined)) },
            },
            { provide: FrontendLoggerService, useValue: { warn: vi.fn(), error: vi.fn() } },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageAccountCardComponent);
    fixture.componentRef.setInput('userForm', createUserManageForm());
    fixture.componentRef.setInput('profileStatus', { key: 'USER_MANAGE.PROFILE_STATUS_SAVED', tone: 'success' });
    fixture.componentRef.setInput('passwordActionState', {
        buttonLabelKey: 'USER_MANAGE.CHANGE_PASSWORD',
        descriptionKey: 'USER_MANAGE.CHANGE_PASSWORD_DESCRIPTION',
    });
    fixture.componentRef.setInput('genderOptions', [{ value: Gender.Male, label: 'Male' }]);
    fixture.componentRef.setInput('languageOptions', [{ value: 'en', label: 'English' }]);
    fixture.componentRef.setInput('themeOptions', [{ value: 'ocean', label: 'Ocean' }]);
    fixture.componentRef.setInput('uiStyleOptions', [{ value: 'classic', label: 'Classic' }]);
    fixture.detectChanges();
    return fixture;
}
