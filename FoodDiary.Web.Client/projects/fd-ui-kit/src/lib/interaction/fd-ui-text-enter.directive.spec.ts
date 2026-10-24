import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiTextEnterDirective } from './fd-ui-text-enter.directive';

@Component({
    imports: [FdUiTextEnterDirective],
    template: '<form (submit)="$event.preventDefault(); submit()"><input (fdUiTextEnter)="send()" /></form>',
})
class TestHost {
    public readonly send = vi.fn();
    public readonly submit = vi.fn();
}

describe('FdUiTextEnterDirective', () => {
    it('leaves composition confirmation untouched and emits the following ordinary Enter once', () => {
        const fixture = TestBed.createComponent(TestHost);
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector('input');
        for (const composition of [{ isComposing: true }, { keyCode: 229 }]) {
            const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true, ...composition });
            input?.dispatchEvent(event);
            expect(event.defaultPrevented).toBe(false);
        }
        expect(fixture.componentInstance.send).not.toHaveBeenCalled();
        const enter = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
        input?.dispatchEvent(enter);
        expect(enter.defaultPrevented).toBe(true);
        expect(fixture.componentInstance.send).toHaveBeenCalledOnce();
        expect(fixture.componentInstance.submit).not.toHaveBeenCalled();
    });
});
