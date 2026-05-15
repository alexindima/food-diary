import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { DietologistPermissions } from '../../../../../shared/models/dietologist.data';
import { DIETOLOGIST_PERMISSION_OPTIONS } from '../../user-manage/user-manage.config';
import { UserManageDietologistPermissionsComponent } from './user-manage-dietologist-permissions.component';

describe('UserManageDietologistPermissionsComponent', () => {
    it('uses configured permission options', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance as UserManageDietologistPermissionsComponent & {
            permissionOptions: typeof DIETOLOGIST_PERMISSION_OPTIONS;
        };

        expect(component.permissionOptions).toEqual(DIETOLOGIST_PERMISSION_OPTIONS);
    });

    it('emits permission changes', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance;
        const permissionChange = vi.fn();
        component.permissionChange.subscribe(permissionChange);

        component.permissionChange.emit({ controlName: 'shareMeals', value: false });

        expect(permissionChange).toHaveBeenCalledWith({ controlName: 'shareMeals', value: false });
    });
});

async function createComponentAsync(): Promise<ComponentFixture<UserManageDietologistPermissionsComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageDietologistPermissionsComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageDietologistPermissionsComponent);
    fixture.componentRef.setInput('permissions', createPermissions());
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
