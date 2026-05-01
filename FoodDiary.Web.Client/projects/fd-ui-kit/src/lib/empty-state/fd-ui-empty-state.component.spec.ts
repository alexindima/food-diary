import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdUiEmptyStateComponent } from './fd-ui-empty-state.component';

@Component({
    imports: [FdUiEmptyStateComponent],
    template: '<fd-ui-empty-state [title]="title" [message]="message" [appearance]="appearance"></fd-ui-empty-state>',
})
class TestHostComponent {
    public title: string | null = 'Nothing here';
    public message = 'Start by adding an item.';
    public appearance: 'default' | 'compact' = 'default';
}

describe('FdUiEmptyStateComponent', () => {
    async function createComponent(appearance: 'default' | 'compact' = 'default'): Promise<ComponentFixture<TestHostComponent>> {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
        }).compileComponents();

        const fixture = TestBed.createComponent(TestHostComponent);
        fixture.componentInstance.appearance = appearance;
        fixture.detectChanges();
        return fixture;
    }

    it('renders title and message', async () => {
        const fixture = await createComponent();
        expect(fixture.nativeElement.textContent).toContain('Nothing here');
        expect(fixture.nativeElement.textContent).toContain('Start by adding an item.');
    });

    it('applies compact appearance class', async () => {
        const fixture = await createComponent('compact');
        const emptyState = fixture.nativeElement.querySelector('fd-ui-empty-state');
        expect(emptyState.classList.contains('fd-ui-empty-state--compact')).toBe(true);
    });
});
