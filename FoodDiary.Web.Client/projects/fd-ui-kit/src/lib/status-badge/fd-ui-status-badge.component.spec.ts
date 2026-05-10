import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdUiStatusBadgeComponent } from './fd-ui-status-badge.component';

@Component({
    imports: [FdUiStatusBadgeComponent],
    template: '<fd-ui-status-badge [tone]="tone">Saved</fd-ui-status-badge>',
})
class TestHostComponent {
    public tone: 'muted' | 'success' | 'warning' | 'danger' = 'muted';
}

describe('FdUiStatusBadgeComponent', () => {
    function host(fixture: ComponentFixture<TestHostComponent>): HTMLElement {
        return fixture.nativeElement as HTMLElement;
    }

    function requireElement<T extends Element>(fixture: ComponentFixture<TestHostComponent>, selector: string): T {
        const element = host(fixture).querySelector<T>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    }

    async function createComponentAsync(
        tone: 'muted' | 'success' | 'warning' | 'danger' = 'muted',
    ): Promise<ComponentFixture<TestHostComponent>> {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
        }).compileComponents();

        const fixture = TestBed.createComponent(TestHostComponent);
        fixture.componentInstance.tone = tone;
        fixture.detectChanges();
        return fixture;
    }

    it('renders projected content', async () => {
        const fixture = await createComponentAsync();
        expect(host(fixture).textContent).toContain('Saved');
    });

    it('applies tone class', async () => {
        const fixture = await createComponentAsync('success');
        const badge = requireElement<HTMLElement>(fixture, 'fd-ui-status-badge');
        expect(badge.classList.contains('fd-ui-status-badge--success')).toBe(true);
    });
});
