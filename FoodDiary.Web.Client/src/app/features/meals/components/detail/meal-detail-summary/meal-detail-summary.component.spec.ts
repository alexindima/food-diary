import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { Meal } from '../../../models/meal.data';
import { MealDetailItemPreviewComponent } from '../meal-detail-item-preview/meal-detail-item-preview.component';
import { MEAL_DETAIL_DEFAULT_QUALITY_GRADE } from '../meal-detail-lib/meal-detail.config';
import type { MealDetailItemPreview, MealMacroBlock, MealSatietyMeta } from '../meal-detail-lib/meal-detail.types';
import { MealDetailSummaryComponent } from './meal-detail-summary.component';

const QUALITY_SCORE = 73;
const DEFAULT_TOTAL_CALORIES = 500;
const DEFAULT_UNKNOWN_QUALITY_SCORE = 50;

describe('MealDetailSummaryComponent', () => {
    it('should compute calories and quality hint from meal', async () => {
        const { component, fixture } = await setupComponentAsync({
            consumption: createMeal({ qualityScore: QUALITY_SCORE, qualityGrade: 'green' }),
        });

        fixture.detectChanges();

        expect(component.calories()).toBe(DEFAULT_TOTAL_CALORIES);
        expect(component.qualityScore()).toBe(QUALITY_SCORE);
        expect(component.qualityGrade()).toBe('green');
        expect(component.qualityHintKey()).toBe('QUALITY.GREEN');
    });

    it('should fall back quality grade and normalize missing quality score', async () => {
        const { component, fixture } = await setupComponentAsync({
            consumption: createMeal({ qualityScore: null, qualityGrade: null }),
        });

        fixture.detectChanges();

        expect(component.qualityGrade()).toBe(MEAL_DETAIL_DEFAULT_QUALITY_GRADE);
        expect(component.qualityScore()).toBe(DEFAULT_UNKNOWN_QUALITY_SCORE);
        expect(component.qualityHintKey()).toBe(`QUALITY.${MEAL_DETAIL_DEFAULT_QUALITY_GRADE.toUpperCase()}`);
    });

    it('should render macros, satiety metadata and comment', async () => {
        const { fixture } = await setupComponentAsync({
            consumption: createMeal({ comment: 'Late lunch' }),
            macroSummaryBlocks: [
                { labelKey: 'NUTRIENTS.PROTEINS', value: 30, unitKey: 'GENERAL.UNITS.GRAM_SHORT', color: '#111', percent: 40 },
            ],
            preMealSatietyMeta: { emoji: '3', title: 'Hungry', description: 'Needs food' },
            postMealSatietyMeta: { emoji: '7', title: 'Full', description: 'Comfortable' },
        });

        fixture.detectChanges();

        const text = getFixtureText(fixture);
        expect(text).toContain('NUTRIENTS.PROTEINS');
        expect(text).toContain('Hungry');
        expect(text).toContain('Comfortable');
        expect(text).toContain('Late lunch');
    });

    it('should pass item preview inputs and emit preview toggle', async () => {
        const itemPreview = createItemPreview();
        const { component, fixture } = await setupComponentAsync({ itemPreview, isItemPreviewExpanded: true });
        const toggleSpy = vi.fn();
        component.itemPreviewExpandedToggle.subscribe(toggleSpy);

        fixture.detectChanges();
        const preview = fixture.debugElement.query(By.directive(MealDetailItemPreviewComponent))
            .componentInstance as MealDetailItemPreviewComponent;
        preview.itemPreviewExpandedToggle.emit();

        expect(preview.items()).toEqual(itemPreview);
        expect(preview.isItemPreviewExpanded()).toBe(true);
        expect(toggleSpy).toHaveBeenCalledOnce();
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        consumption: Meal;
        isItemPreviewExpanded: boolean;
        itemPreview: readonly MealDetailItemPreview[];
        macroSummaryBlocks: readonly MealMacroBlock[];
        postMealSatietyMeta: MealSatietyMeta;
        preMealSatietyMeta: MealSatietyMeta;
    }> = {},
): Promise<{
    component: MealDetailSummaryComponent;
    fixture: ComponentFixture<MealDetailSummaryComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealDetailSummaryComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealDetailSummaryComponent);
    fixture.componentRef.setInput('consumption', overrides.consumption ?? createMeal());
    fixture.componentRef.setInput('macroSummaryBlocks', overrides.macroSummaryBlocks ?? []);
    fixture.componentRef.setInput('preMealSatietyMeta', overrides.preMealSatietyMeta ?? createSatietyMeta('Before'));
    fixture.componentRef.setInput('postMealSatietyMeta', overrides.postMealSatietyMeta ?? createSatietyMeta('After'));
    fixture.componentRef.setInput('itemPreview', overrides.itemPreview ?? createItemPreview());
    fixture.componentRef.setInput('isItemPreviewExpanded', overrides.isItemPreviewExpanded ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createMeal(overrides: Partial<Meal> = {}): Meal {
    return {
        id: 'meal-1',
        date: '2026-05-14T12:00:00Z',
        mealType: 'LUNCH',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: DEFAULT_TOTAL_CALORIES,
        totalProteins: 30,
        totalFats: 20,
        totalCarbs: 50,
        totalFiber: 5,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        qualityScore: QUALITY_SCORE,
        qualityGrade: 'yellow',
        items: [],
        aiSessions: [],
        ...overrides,
    };
}

function getFixtureText(fixture: ComponentFixture<MealDetailSummaryComponent>): string {
    const host = fixture.nativeElement as HTMLElement;
    return host.textContent;
}

function createSatietyMeta(title: string): MealSatietyMeta {
    return {
        emoji: '5',
        title,
        description: `${title} description`,
    };
}

function createItemPreview(): MealDetailItemPreview[] {
    return [{ name: 'Chicken', amount: 100, unitKey: 'PRODUCT_AMOUNT_UNITS.G', unitText: null }];
}
