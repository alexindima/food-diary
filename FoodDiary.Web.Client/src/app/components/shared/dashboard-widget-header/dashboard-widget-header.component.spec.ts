import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { DashboardWidgetHeaderComponent } from './dashboard-widget-header.component';

async function setupDashboardWidgetHeaderAsync(): Promise<ComponentFixture<DashboardWidgetHeaderComponent>> {
    await TestBed.configureTestingModule({
        imports: [DashboardWidgetHeaderComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardWidgetHeaderComponent);
    fixture.componentRef.setInput('title', 'Title');
    return fixture;
}

describe('DashboardWidgetHeaderComponent', () => {
    it('detects icon from icon name or label', async () => {
        const fixture = await setupDashboardWidgetHeaderAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        expect(component.hasIcon()).toBe(false);

        fixture.componentRef.setInput('iconLabel', 'A');
        fixture.detectChanges();
        expect(component.hasIcon()).toBe(true);

        fixture.componentRef.setInput('iconLabel', '');
        fixture.componentRef.setInput('iconName', 'restaurant');
        fixture.detectChanges();
        expect(component.hasIcon()).toBe(true);
    });
});
