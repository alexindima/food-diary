import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiCheckboxComponent } from './fd-ui-checkbox';

const dispatchCheckboxChange = (checked: boolean): Event => {
    const input = document.createElement('input');
    input.type = 'checkbox';
    input.checked = checked;
    const event = new Event('change');
    input.dispatchEvent(event);
    return event;
};

describe('FdUiCheckboxComponent', () => {
    let component: FdUiCheckboxComponent;
    let fixture: ComponentFixture<FdUiCheckboxComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = host().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiCheckboxComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiCheckboxComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render label text', () => {
        fixture.componentRef.setInput('label', 'Accept terms');
        fixture.detectChanges();

        const checkboxEl = requireElement('.fd-ui-checkbox__label');
        expect(checkboxEl.textContent.trim()).toContain('Accept terms');
    });

    it('should render hint when provided', () => {
        fixture.componentRef.setInput('hint', 'Please read carefully');
        fixture.detectChanges();

        const hintEl = requireElement('.fd-ui-checkbox__hint');
        expect(hintEl.textContent.trim()).toBe('Please read carefully');
    });

    it('should write checked state from model', () => {
        component.checked.set(true);
        fixture.detectChanges();
        expect(component['checkedValue']).toBe(true);

        component.checked.set(false);
        fixture.detectChanges();
        expect(component['checkedValue']).toBe(false);
    });

    it('should update checked model when checkbox changes', () => {
        const changeEvent = dispatchCheckboxChange(true);
        component['updateCheckedValue'](changeEvent);

        expect(component.checked()).toBe(true);

        const uncheckEvent = dispatchCheckboxChange(false);
        component['updateCheckedValue'](uncheckEvent);

        expect(component.checked()).toBe(false);
    });

    it('should mark touched on blur', () => {
        component['touchControl']();

        expect(component.touched()).toBe(true);
    });

    it('should set disabled state from input', () => {
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(true);

        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(false);
    });
});
