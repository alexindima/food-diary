import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiPieChartComponent } from './fd-ui-pie-chart.component';

describe('FdUiPieChartComponent', () => {
    let component: FdUiPieChartComponent;
    let fixture: ComponentFixture<FdUiPieChartComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiPieChartComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiPieChartComponent);
        component = fixture.componentInstance;
    });

    it('should render chart segments and total', () => {
        fixture.componentRef.setInput('title', 'Browsers');
        fixture.componentRef.setInput('segments', [
            { label: 'Opera', value: 8 },
            { label: 'Chrome', value: 2 },
        ]);
        fixture.detectChanges();

        expect(component.total()).toBe(10);
        expect(host().querySelectorAll('.fd-ui-pie-chart__segment')).toHaveLength(2);
        expect(host().querySelector('.fd-ui-pie-chart__center strong')?.textContent?.trim()).toBe('Browsers');
        expect(host().querySelector('.fd-ui-pie-chart__segment title')?.textContent?.trim()).toBe('Opera: 8');
        expect(host().querySelector('.fd-ui-pie-chart__legend-item')?.getAttribute('title')).toBe('Opera: 8');
        expect(component.ariaLabel()).toContain('Browsers: Opera 8, Chrome 2');
    });

    it('should show empty state when there are no positive values', () => {
        fixture.componentRef.setInput('segments', [{ label: 'Desktop', value: 0 }]);
        fixture.detectChanges();

        expect(component.total()).toBe(0);
        expect(host().querySelectorAll('.fd-ui-pie-chart__segment')).toHaveLength(0);
        expect(host().querySelector('.fd-ui-pie-chart__empty')?.textContent?.trim()).toBe('No data');
    });
});
