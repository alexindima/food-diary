import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import { createUserManageFormModel } from '../../user-manage/user-manage-lib/user-manage-form.mapper';
import { UserManageBodyCardComponent } from './user-manage-body-card';

const HEIGHT_CM = 180;
const UPDATED_INCHES = 10;

describe('UserManageBodyCardComponent', () => {
    it('renders body fields from the provided form and options', async () => {
        const fixture = await createComponentAsync();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.BODY_SECTION');
    });

    it('renders imperial height fields and emits canonical centimeters', async () => {
        const fixture = await createComponentAsync();
        const measurements = TestBed.inject(MeasurementSystemService);
        const patch = vi.fn();
        fixture.componentInstance['userFormPatch'].subscribe(patch);
        fixture.componentInstance['userForm']().heightCm().value.set(HEIGHT_CM);

        measurements.setSystem('imperial');
        fixture.detectChanges();
        fixture.componentInstance['onImperialHeightChange']('inches', UPDATED_INCHES);

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[data-user-field="height-feet"]')).not.toBeNull();
        expect(host.querySelector('[data-user-field="height-inches"]')).not.toBeNull();
        expect(patch).toHaveBeenLastCalledWith({ heightCm: 177.8 });
    });
});

async function createComponentAsync(): Promise<ComponentFixture<UserManageBodyCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageBodyCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageBodyCardComponent);
    fixture.componentRef.setInput(
        'userForm',
        TestBed.runInInjectionContext(() => form(signal(createUserManageFormModel()))),
    );
    fixture.componentRef.setInput('activityLevelOptions', [{ value: 'MODERATE', label: 'Moderate' }]);
    fixture.detectChanges();
    return fixture;
}
