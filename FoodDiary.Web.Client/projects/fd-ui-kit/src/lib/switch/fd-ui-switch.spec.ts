import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiSwitchComponent } from './fd-ui-switch';

describe('FdUiSwitchComponent', () => {
    let fixture: ComponentFixture<FdUiSwitchComponent>;
    let component: FdUiSwitchComponent;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiSwitchComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiSwitchComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('renders the controlled checked state', () => {
        fixture.componentRef.setInput('checked', true);
        fixture.detectChanges();

        const button = fixture.debugElement.query(By.css('.fd-ui-switch')).nativeElement as HTMLButtonElement;

        expect(button.getAttribute('aria-checked')).toBe('true');
    });

    it('emits the requested checked state on click', () => {
        const checkedChange = vi.fn();
        component['checkedChange'].subscribe(checkedChange);
        const button = fixture.debugElement.query(By.css('.fd-ui-switch')).nativeElement as HTMLButtonElement;

        button.click();
        fixture.detectChanges();

        expect(checkedChange).toHaveBeenCalledWith(true);
        expect(button.getAttribute('aria-checked')).toBe('false');
    });

    it('does not emit when disabled', () => {
        const checkedChange = vi.fn();
        component['checkedChange'].subscribe(checkedChange);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        const button = fixture.debugElement.query(By.css('.fd-ui-switch')).nativeElement as HTMLButtonElement;
        button.click();
        fixture.detectChanges();

        expect(checkedChange).not.toHaveBeenCalled();
        expect(button.getAttribute('aria-checked')).toBe('false');
    });

    it('uses a css variable for disabled opacity', () => {
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        const button = fixture.debugElement.query(By.css('.fd-ui-switch')).nativeElement as HTMLButtonElement;

        expect(button.classList.contains('fd-ui-switch--disabled')).toBe(true);
    });

    it('uses tokenized focus styling instead of the native outline', () => {
        const button = fixture.debugElement.query(By.css('.fd-ui-switch')).nativeElement as HTMLButtonElement;

        button.focus();
        fixture.detectChanges();

        expect(getComputedStyle(button).outlineStyle).toBe('none');
    });
});
