import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { DietologistPermissions } from '../../../../../shared/models/dietologist.data';
import { DIETOLOGIST_PERMISSION_OPTIONS } from '../../user-manage/user-manage-lib/user-manage.config';
import { UserManageDietologistPermissionsComponent } from './user-manage-dietologist-permissions';

describe('UserManageDietologistPermissionsComponent', () => {
    it('uses configured permission options', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance as UserManageDietologistPermissionsComponent & {
            permissionOptions: typeof DIETOLOGIST_PERMISSION_OPTIONS;
        };

        expect(component['permissionOptions']).toEqual(DIETOLOGIST_PERMISSION_OPTIONS);
    });

    it('emits permission changes', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance;
        const permissionChange = vi.fn();
        component['permissionChange'].subscribe(permissionChange);

        component['permissionChange'].emit({ controlName: 'shareMeals', value: false });

        expect(permissionChange).toHaveBeenCalledWith({ controlName: 'shareMeals', value: false });
    });

    it('keeps disabled autosave switches visually stable', async () => {
        const fixture = await createComponentAsync(true);
        const host = fixture.nativeElement as HTMLElement;
        const switchComponent = host.querySelector<HTMLElement>('fd-ui-switch');
        const switchButton = host.querySelector<HTMLButtonElement>('button[role="switch"]');

        expect(switchComponent).not.toBeNull();
        expect(switchButton).not.toBeNull();
        expect(switchComponent?.classList.contains('user-manage__dietologist-permission-switch')).toBe(true);
        expect(switchButton?.disabled).toBe(true);
        expect(switchButton?.classList.contains('fd-ui-switch--disabled')).toBe(true);
    });
});

async function createComponentAsync(isSavingDietologist = false): Promise<ComponentFixture<UserManageDietologistPermissionsComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageDietologistPermissionsComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageDietologistPermissionsComponent);
    fixture.componentRef.setInput('permissions', createPermissions());
    fixture.componentRef.setInput('isSavingDietologist', isSavingDietologist);
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
