import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { email, form, required } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { DietologistPermissions } from '../../../../../shared/models/dietologist.data';
import type { DietologistPermissionChange } from '../../user-manage/user-manage-lib/user-manage.types';
import { createDietologistFormModel } from '../../user-manage/user-manage-lib/user-manage-form.mapper';
import { UserManageDietologistCardComponent } from './user-manage-dietologist-card';

describe('UserManageDietologistCardComponent', () => {
    it('routes profile permission toggle to profile event and other permissions to generic event', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance;
        const profileToggle = vi.fn();
        const permissionChange = vi.fn();
        component['dietologistProfileToggle'].subscribe(profileToggle);
        component['dietologistPermissionChange'].subscribe(permissionChange);
        const protectedComponent = component as unknown as {
            changeDietologistPermission: (change: DietologistPermissionChange) => void;
        };

        protectedComponent['changeDietologistPermission']({ controlName: 'shareProfile', value: false });
        protectedComponent['changeDietologistPermission']({ controlName: 'shareMeals', value: false });

        expect(profileToggle).toHaveBeenCalledWith(false);
        expect(permissionChange).toHaveBeenCalledWith({ controlName: 'shareMeals', value: false });
    });
});

async function createComponentAsync(): Promise<ComponentFixture<UserManageDietologistCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageDietologistCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageDietologistCardComponent);
    fixture.componentRef.setInput(
        'dietologistForm',
        TestBed.runInInjectionContext(() =>
            form(signal(createDietologistFormModel()), path => {
                required(path.email);
                email(path.email);
            }),
        ),
    );
    fixture.componentRef.setInput('dietologistRelationship', null);
    fixture.componentRef.setInput('dietologistPermissions', createPermissions());
    fixture.componentRef.setInput('dietologistError', null);
    fixture.componentRef.setInput('dietologistInviteEmailError', null);
    fixture.componentRef.setInput('isLoadingDietologist', false);
    fixture.componentRef.setInput('isSavingDietologist', false);
    fixture.detectChanges();
    return fixture;
}

function createPermissions(): DietologistPermissions {
    return {
        shareProfile: true,
        shareMeals: true,
        shareStatistics: true,
        shareWeight: true,
        shareWaist: true,
        shareGoals: true,
        shareHydration: true,
        shareFasting: true,
    };
}
