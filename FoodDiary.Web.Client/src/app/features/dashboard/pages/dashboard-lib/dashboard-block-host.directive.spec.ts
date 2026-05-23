import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { DashboardBlockContentDirective, DashboardBlockHostDirective } from './dashboard-block-host.directive';
import type { DashboardBlockState } from './dashboard-view.types';

describe('DashboardBlockHostDirective', () => {
    it('applies block state attributes and emits activation for keyboard controls', () => {
        const fixture = setupFixture();
        const hostElement = fixture.nativeElement as HTMLElement;
        const host = hostElement.querySelector('[fdDashboardBlockHost]') as HTMLElement;
        const spaceEvent = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });

        host.dispatchEvent(spaceEvent);

        expect(host.getAttribute('role')).toBe('button');
        expect(host.getAttribute('tabindex')).toBe('0');
        expect(host.getAttribute('aria-pressed')).toBe('true');
        expect(host.getAttribute('aria-label')).toBe('Toggle block');
        expect(host.classList.contains('dashboard__block--hidden')).toBe(false);
        expect(spaceEvent.defaultPrevented).toBe(true);
        expect(fixture.componentInstance.activated).toHaveBeenCalledTimes(1);

        host.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

        expect(fixture.componentInstance.activated).toHaveBeenCalledTimes(2);
    });

    it('marks nested dashboard content inert and hidden while the block is editable', () => {
        const fixture = setupFixture();
        const hostElement = fixture.nativeElement as HTMLElement;
        const content = hostElement.querySelector('[fdDashboardBlockContent]') as HTMLElement;

        expect(content.getAttribute('inert')).toBe('');
        expect(content.getAttribute('aria-hidden')).toBe('true');

        fixture.componentInstance.state.update(state => ({ ...state, inert: null }));
        fixture.detectChanges();

        expect(content.getAttribute('inert')).toBeNull();
        expect(content.getAttribute('aria-hidden')).toBeNull();
    });
});

function setupFixture(): ComponentFixture<DirectiveHostComponent> {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        imports: [DirectiveHostComponent],
    });

    const fixture = TestBed.createComponent(DirectiveHostComponent);
    fixture.detectChanges();
    return fixture;
}

@Component({
    imports: [DashboardBlockContentDirective, DashboardBlockHostDirective],
    template: `
        <div fdDashboardBlockHost [state]="state()" (blockActivated)="activated($event)">
            <div fdDashboardBlockContent [state]="state()">Block</div>
        </div>
    `,
})
class DirectiveHostComponent {
    public readonly activated = vi.fn();
    public readonly state = signal<DashboardBlockState>({
        hidden: false,
        role: 'button',
        tabIndex: 0,
        ariaPressed: true,
        ariaDisabled: null,
        ariaLabel: 'Toggle block',
        inert: '',
    });
}
