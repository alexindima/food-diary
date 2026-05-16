import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { DashboardWidgetFrameComponent } from './dashboard-widget-frame.component';

describe('DashboardWidgetFrameComponent', () => {
    it('renders projected content with header title', async () => {
        await TestBed.configureTestingModule({
            imports: [DashboardWidgetFrameComponent],
        }).compileComponents();

        const fixture: ComponentFixture<DashboardWidgetFrameComponent> = TestBed.createComponent(DashboardWidgetFrameComponent);
        fixture.componentRef.setInput('title', 'Widget title');
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).textContent).toContain('Widget title');
    });
});
