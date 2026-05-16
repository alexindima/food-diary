import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { NutrientsSummaryComponent } from './nutrients-summary.component';

const CALORIES = 320;
const PROTEINS = 24;
const FATS = 12;
const CARBS = 36;
const FIBER = 5;
const ALCOHOL = 2;
const CUSTOM_GAP = 4;

async function setupNutrientsSummaryAsync(): Promise<ComponentFixture<NutrientsSummaryComponent>> {
    await TestBed.configureTestingModule({
        imports: [NutrientsSummaryComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(NutrientsSummaryComponent);
    fixture.componentRef.setInput('calories', CALORIES);
    fixture.componentRef.setInput('nutrientChartData', { proteins: PROTEINS, fats: FATS, carbs: CARBS });
    return fixture;
}

describe('NutrientsSummaryComponent', () => {
    it('renders nutrient values and optional highlights', async () => {
        const fixture = await setupNutrientsSummaryAsync();
        fixture.componentRef.setInput('fiberValue', FIBER);
        fixture.componentRef.setInput('alcoholValue', ALCOHOL);
        fixture.detectChanges();

        const text = (fixture.nativeElement as HTMLElement).textContent;

        expect(text).toContain('SHARED.NUTRIENTS_SUMMARY.FIBER');
        expect(text).toContain('NUTRIENTS.ALCOHOL');
        expect(text).toContain(PROTEINS.toString());
        expect(text).toContain(FATS.toString());
        expect(text).toContain(CARBS.toString());
    });

    it('hides charts when all macro values are empty', async () => {
        const fixture = await setupNutrientsSummaryAsync();
        fixture.componentRef.setInput('nutrientChartData', { proteins: 0, fats: 0, carbs: 0 });
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('fd-nutrients-summary-charts')).toBeNull();
    });

    it('merges custom config into computed styles', async () => {
        const fixture = await setupNutrientsSummaryAsync();
        fixture.componentRef.setInput('config', { styles: { common: { gap: CUSTOM_GAP } } });
        fixture.detectChanges();

        expect(fixture.componentInstance.summaryWrapperStyles()).toEqual({ gap: `${CUSTOM_GAP}px` });
    });
});
