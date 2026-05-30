import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiBarChartComponent } from './fd-ui-bar-chart';

const PROTEIN_VALUE = 50;
const FAT_VALUE = 25;
const BAR_COUNT = 2;
const ZERO_HEIGHT = 0;
const GRID_LINE_COUNT = 5;

describe('FdUiBarChartComponent', () => {
    let component: FdUiBarChartComponent;
    let fixture: ComponentFixture<FdUiBarChartComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiBarChartComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiBarChartComponent);
        component = fixture.componentInstance;
    });

    it('renders proportional bars and labels', () => {
        fixture.componentRef.setInput('title', 'Macros');
        fixture.componentRef.setInput('items', [
            { label: 'Protein', value: PROTEIN_VALUE },
            { label: 'Fat', value: FAT_VALUE },
        ]);
        fixture.detectChanges();

        const bars = host().querySelectorAll('.fd-ui-bar-chart__bar');
        const firstBar = bars.item(0);
        const secondBar = bars.item(1);

        expect(component['maxValue']()).toBe(PROTEIN_VALUE);
        expect(bars).toHaveLength(BAR_COUNT);
        expect(host().querySelectorAll('.fd-ui-bar-chart__grid-line')).toHaveLength(GRID_LINE_COUNT);
        expect(Number(firstBar.getAttribute('height'))).toBeGreaterThan(Number(secondBar.getAttribute('height')));
        expect(getText('.fd-ui-bar-chart__label')).toBe('Protein');
        expect(component['ariaLabel']()).toBe('Macros: Protein 50, Fat 25');
    });

    it('keeps zero value categories visible', () => {
        fixture.componentRef.setInput('items', [{ label: 'Calories', value: 0 }]);
        fixture.detectChanges();

        expect(component['maxValue']()).toBe(0);
        expect(host().querySelectorAll('.fd-ui-bar-chart__bar')).toHaveLength(1);
        expect(Number(host().querySelector('.fd-ui-bar-chart__bar')?.getAttribute('height'))).toBe(ZERO_HEIGHT);
        expect(getText('.fd-ui-bar-chart__label')).toBe('Calories');
    });

    it('shows empty state without items', () => {
        fixture.componentRef.setInput('items', []);
        fixture.detectChanges();

        expect(getText('.fd-ui-bar-chart__empty')).toBe('No data');
    });

    function getText(selector: string): string {
        const element = host().querySelector(selector);
        if (element === null) {
            throw new Error(`Expected ${selector} to exist`);
        }

        return element.textContent.trim();
    }
});
