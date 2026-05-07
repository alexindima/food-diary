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
        expect(fixture.nativeElement.textContent).toContain('Saved');
    });

    it('applies tone class', async () => {
        const fixture = await createComponentAsync('success');
        const badge = fixture.nativeElement.querySelector('fd-ui-status-badge');
        expect(badge.classList.contains('fd-ui-status-badge--success')).toBe(true);
    });
});
