import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ImageUploadFacade } from '../../../../../shared/lib/image-upload.facade';
import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import type { UserFormValues } from '../../../lib/user-manage.types';
import { createUserManageFormModel } from '../../../lib/user-manage-form.mapper';
import { UserManageComparisonWidgetsComponent } from './user-manage-comparison-widgets';

let fixture: ComponentFixture<UserManageComparisonWidgetsComponent>;
let userForm: FieldTree<UserFormValues>;
let input: HTMLInputElement;
let patched: Mock<(patch: Partial<UserFormValues>) => void>;
let originalMeasurementSystem: string | null;
const originalScrollIntoView = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollIntoView');
const originalHeightCm = 170;
const fullHeightCm = 170.2;
const feetOnlyHeightCm = 152.4;
const inchesOnlyHeightCm = 17.8;
const tallerHeightCm = 180;

beforeEach(() => {
    originalMeasurementSystem = localStorage.getItem('fd_measurement_system');
    Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
    vi.spyOn(Intl, 'supportedValuesOf').mockReturnValue(['America/New_York', 'Asia/Tbilisi', 'Europe/Paris']);
    TestBed.configureTestingModule({
        imports: [UserManageComparisonWidgetsComponent],
        providers: [provideTranslateTesting(), provideRouter([]), { provide: ImageUploadFacade, useValue: {} }],
    });
    userForm = TestBed.runInInjectionContext(() => form(signal({ ...createUserManageFormModel(), timeZoneId: 'Asia/Tbilisi' })));
    fixture = TestBed.createComponent(UserManageComparisonWidgetsComponent);
    fixture.componentRef.setInput('userForm', userForm);
    for (const name of ['genderOptions', 'languageOptions', 'themeOptions', 'uiStyleOptions', 'activityLevelOptions']) {
        fixture.componentRef.setInput(name, []);
    }
    fixture.componentRef.setInput('currentWeight', null);
    fixture.componentRef.setInput('currentWaist', null);
    patched = vi.fn();
    fixture.componentInstance.userFormPatch.subscribe(patch => {
        patched(patch);
        if ('timeZoneId' in patch && patch.timeZoneId !== undefined) {
            userForm.timeZoneId().value.set(patch.timeZoneId);
        }
        if ('heightCm' in patch && patch.heightCm !== undefined) {
            userForm.heightCm().value.set(patch.heightCm);
        }
    });
    fixture.detectChanges();
    const control = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('fd-ui-autocomplete input');
    if (control === null) {
        throw new Error('Time zone search input was not rendered');
    }
    input = control;
});

afterEach(() => {
    if (originalMeasurementSystem === null) {
        localStorage.removeItem('fd_measurement_system');
    } else {
        localStorage.setItem('fd_measurement_system', originalMeasurementSystem);
    }
    vi.restoreAllMocks();
    if (originalScrollIntoView === undefined) {
        Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
    } else {
        Object.defineProperty(Element.prototype, 'scrollIntoView', originalScrollIntoView);
    }
});

describe('Profile time zone saved selection', () => {
    it('preserves the saved zone without dirtying the profile when Enter follows focus alone', async () => {
        input.focus();
        fixture.detectChanges();
        await fixture.whenStable();

        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        fixture.detectChanges();

        expect(input.value).toBe('Asia/Tbilisi');
        expect(userForm.timeZoneId().value()).toBe('Asia/Tbilisi');
        expect(userForm().dirty()).toBe(false);
        expect(patched).not.toHaveBeenCalled();
    });
});

describe('Profile time zone search', () => {
    it('filters city names without changing or dirtying the saved time zone', () => {
        search('NEW YORK');

        expect(optionLabels()).toEqual(['America/New_York']);
        expect(userForm.timeZoneId().value()).toBe('Asia/Tbilisi');
        expect(userForm().dirty()).toBe(false);
        expect(patched).not.toHaveBeenCalled();
    });

    it('restores the selected time zone when a search is cancelled or blurred', () => {
        search('Paris');
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        fixture.detectChanges();

        expect(input.value).toBe('Asia/Tbilisi');
        expect(input.getAttribute('aria-expanded')).toBe('false');

        search('no matching zone');
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain('USER_MANAGE.TIME_ZONE_NO_RESULTS');
        input.dispatchEvent(new FocusEvent('blur'));
        fixture.detectChanges();

        expect(input.value).toBe('Asia/Tbilisi');
        expect(userForm.timeZoneId().value()).toBe('Asia/Tbilisi');
        expect(userForm().dirty()).toBe(false);
        expect(patched).not.toHaveBeenCalled();
    });

    it('commits only the chosen option when selected with the keyboard', () => {
        search('Paris');
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        fixture.detectChanges();

        expect(patched).toHaveBeenCalledExactlyOnceWith({ timeZoneId: 'Europe/Paris' });
        expect(userForm.timeZoneId().value()).toBe('Europe/Paris');
        expect(input.value).toBe('Europe/Paris');
        expect(input.getAttribute('aria-expanded')).toBe('false');
    });

    it('can select with Enter after correcting a search with no matches', () => {
        search('no matching zone');
        search('Paris');
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        fixture.detectChanges();

        expect(patched).toHaveBeenCalledExactlyOnceWith({ timeZoneId: 'Europe/Paris' });
        expect(input.value).toBe('Europe/Paris');
    });

    it('commits a mouse selection without losing it when focus returns to the field', () => {
        search('New York');
        const option = document.querySelector<HTMLButtonElement>('[role="option"]');
        if (option === null) {
            throw new Error('Matching time zone option was not rendered');
        }
        input.dispatchEvent(new FocusEvent('blur', { relatedTarget: option }));
        option.click();
        fixture.detectChanges();

        expect(patched).toHaveBeenCalledExactlyOnceWith({ timeZoneId: 'America/New_York' });
        expect(input.value).toBe('America/New_York');
        expect(document.activeElement).toBe(input);
        expect(input.getAttribute('aria-expanded')).toBe('false');
    });

    it('keeps a saved IANA alias selectable when the browser does not list it', () => {
        userForm.timeZoneId().value.set('Europe/Kyiv');
        fixture.detectChanges();
        search('Kyiv');

        expect(optionLabels()).toEqual(['Europe/Kyiv']);
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        fixture.detectChanges();
        expect(input.value).toBe('Europe/Kyiv');
        expect(patched).not.toHaveBeenCalled();
    });
});

describe('Profile imperial height editing', () => {
    beforeEach(() => {
        userForm.heightCm().value.set(fullHeightCm);
        TestBed.inject(MeasurementSystemService).setSystem('imperial');
        fixture.detectChanges();
        patched.mockClear();
    });

    it('keeps feet while clearing and retyping inches with live parent form feedback', () => {
        editImperialPart(1, '');

        expect(imperialValues()).toEqual(['5', '']);
        expect(userForm.heightCm().value()).toBe(feetOnlyHeightCm);

        editImperialPart(1, '7');

        expect(imperialValues()).toEqual(['5', '7']);
        expect(userForm.heightCm().value()).toBe(fullHeightCm);
        expect(patched).toHaveBeenLastCalledWith({ heightCm: fullHeightCm });
    });

    it('keeps inches while clearing and retyping feet', () => {
        editImperialPart(0, '');

        expect(imperialValues()).toEqual(['', '7']);
        expect(userForm.heightCm().value()).toBe(inchesOnlyHeightCm);

        editImperialPart(0, '5');

        expect(imperialValues()).toEqual(['5', '7']);
        expect(userForm.heightCm().value()).toBe(fullHeightCm);
    });

    it('clears unknown height only when both parts are empty and accepts a parent reset', () => {
        editImperialPart(1, '');
        editImperialPart(0, '');

        expect(imperialValues()).toEqual(['', '']);
        expect(userForm.heightCm().value()).toBeNull();

        userForm.heightCm().value.set(originalHeightCm);
        fixture.detectChanges();

        expect(imperialValues()).toEqual(['5', '6.9']);
    });

    it('replaces an old draft when authoritative metric height changes', () => {
        editImperialPart(0, '');
        userForm.heightCm().value.set(tallerHeightCm);
        fixture.detectChanges();

        expect(imperialValues()).toEqual(['5', '10.9']);

        userForm.heightCm().value.set(inchesOnlyHeightCm);
        fixture.detectChanges();

        expect(imperialValues()).toEqual(['0', '7']);
    });
});

function imperialInputs(): HTMLInputElement[] {
    return Array.from(
        (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement>('.profile-comparison__imperial-height input'),
    );
}

function imperialValues(): string[] {
    return imperialInputs().map(control => control.value);
}

function editImperialPart(index: number, value: string): void {
    const control = imperialInputs().at(index);
    if (control === undefined) {
        throw new Error('Imperial height part is not rendered');
    }
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
}

function search(query: string): void {
    input.focus();
    input.value = query;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
}

function optionLabels(): string[] {
    return Array.from(document.querySelectorAll('[role="option"]'), option => option.textContent.trim());
}
