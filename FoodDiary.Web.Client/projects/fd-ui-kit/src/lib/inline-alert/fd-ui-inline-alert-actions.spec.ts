import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiInlineAlertComponent } from './fd-ui-inline-alert';

describe('FdUiInlineAlert actions', () => {
    it('preserves two named native buttons and routes each click to its own output', () => {
        TestBed.configureTestingModule({ imports: [FdUiInlineAlertComponent] });
        const fixture = TestBed.createComponent(FdUiInlineAlertComponent);
        fixture.componentRef.setInput('severity', 'warning');
        fixture.componentRef.setInput('primaryActionLabel', 'Snooze');
        fixture.componentRef.setInput('secondaryActionLabel', 'Dismiss');
        fixture.detectChanges();
        const primary = vi.fn();
        const secondary = vi.fn();
        fixture.componentInstance.primaryAction.subscribe(primary);
        fixture.componentInstance.secondaryAction.subscribe(secondary);
        const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button');
        expect(buttons).toHaveLength(2);
        expect(buttons[0].type).toBe('button');
        expect(buttons[0].textContent).toContain('Snooze');
        expect(buttons[1].textContent).toContain('Dismiss');
        buttons[0].focus();
        expect(document.activeElement).toBe(buttons[0]);
        buttons[0].click();
        expect(primary).toHaveBeenCalledOnce();
        expect(secondary).not.toHaveBeenCalled();
        buttons[1].click();
        expect(secondary).toHaveBeenCalledOnce();
    });

    it('removes a hidden action while keeping the remaining button named and focusable', () => {
        TestBed.configureTestingModule({ imports: [FdUiInlineAlertComponent] });
        const fixture = TestBed.createComponent(FdUiInlineAlertComponent);
        fixture.componentRef.setInput('primaryActionLabel', 'Retry');
        fixture.componentRef.setInput('secondaryActionLabel', 'Dismiss');
        fixture.detectChanges();
        fixture.componentRef.setInput('primaryActionLabel', null);
        fixture.detectChanges();
        const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button');
        expect(buttons).toHaveLength(1);
        expect(buttons[0].textContent).toContain('Dismiss');
        buttons[0].focus();
        expect(document.activeElement).toBe(buttons[0]);
    });
});
