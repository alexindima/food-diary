import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { DietologistPermissions } from '../../../../../shared/models/dietologist.data';
import type { DietologistPermissionChange } from '../../user-manage/user-manage-lib/user-manage.types';
import { createDietologistForm } from '../../user-manage/user-manage-lib/user-manage-form.mapper';
import { UserManageDietologistCardComponent } from './user-manage-dietologist-card.component';

describe('UserManageDietologistCardComponent', () => {
    it('routes profile permission toggle to profile event and other permissions to generic event', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance;
        const profileToggle = vi.fn();
        const permissionChange = vi.fn();
        component.dietologistProfileToggle.subscribe(profileToggle);
        component.dietologistPermissionChange.subscribe(permissionChange);
        const protectedComponent = component as unknown as {
            handleDietologistPermissionChange: (change: DietologistPermissionChange) => void;
        };

        protectedComponent.handleDietologistPermissionChange({ controlName: 'shareProfile', value: false });
        protectedComponent.handleDietologistPermissionChange({ controlName: 'shareMeals', value: false });

        expect(profileToggle).toHaveBeenCalledWith(false);
        expect(permissionChange).toHaveBeenCalledWith({ controlName: 'shareMeals', value: false });
    });
});

async function createComponentAsync(): Promise<ComponentFixture<UserManageDietologistCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageDietologistCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageDietologistCardComponent);
    fixture.componentRef.setInput('dietologistForm', createDietologistForm());
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
