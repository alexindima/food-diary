import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { createUserManageForm } from '../../user-manage/user-manage-lib/user-manage-form.mapper';
import { UserManageBodyCardComponent } from './user-manage-body-card.component';

describe('UserManageBodyCardComponent', () => {
    it('renders body fields from the provided form and options', async () => {
        const fixture = await createComponentAsync();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.BODY_SECTION');
    });
});

async function createComponentAsync(): Promise<ComponentFixture<UserManageBodyCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageBodyCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageBodyCardComponent);
    fixture.componentRef.setInput('userForm', createUserManageForm());
    fixture.componentRef.setInput('activityLevelOptions', [{ value: 'MODERATE', label: 'Moderate' }]);
    fixture.detectChanges();
    return fixture;
}
