import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiSwitchComponent } from './fd-ui-switch.component';

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

    it('toggles checked state on click', () => {
        const checkedChangeSpy = vi.fn();
        component.checkedChange.subscribe(checkedChangeSpy);
        const button = fixture.debugElement.query(By.css('.fd-ui-switch')).nativeElement as HTMLButtonElement;

        button.click();
        fixture.detectChanges();

        expect(checkedChangeSpy).toHaveBeenCalledWith(true);
        expect(button.getAttribute('aria-checked')).toBe('false');
    });

    it('does not toggle when disabled', () => {
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        const button = fixture.debugElement.query(By.css('.fd-ui-switch')).nativeElement as HTMLButtonElement;
        button.click();
        fixture.detectChanges();

        expect(button.getAttribute('aria-checked')).toBe('false');
    });
});
