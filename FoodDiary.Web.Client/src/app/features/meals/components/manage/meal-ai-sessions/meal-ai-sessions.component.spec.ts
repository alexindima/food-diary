import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { ConsumptionAiItemManageDto, ConsumptionAiSessionManageDto } from '../../../models/meal.data';
import { MealAiSessionsComponent } from './meal-ai-sessions.component';

describe('MealAiSessionsComponent rows', () => {
    it('should build collapsed AI session rows with preview items and totals', async () => {
        const { component } = await setupComponentAsync([createSession()]);

        expect(component.aiSessionRows()).toEqual([
            expect.objectContaining({
                index: 0,
                itemCount: 3,
                isExpanded: false,
                hiddenItemsCount: 1,
                visibleItems: [
                    { nameLabel: 'Apple', amountLabel: '100 GENERAL.UNITS.G' },
                    { nameLabel: 'Milk', amountLabel: '200 GENERAL.UNITS.ML' },
                ],
                caloriesLabel: '210 GENERAL.UNITS.KCAL',
                proteinsLabel: '11 GENERAL.UNITS.G',
                fatsLabel: '5 GENERAL.UNITS.G',
                carbsLabel: '35 GENERAL.UNITS.G',
            }),
        ]);
    });

    it('should include all items when session is expanded', async () => {
        const { component } = await setupComponentAsync([createSession()]);

        component.toggleAiSessionExpanded(0);

        expect(component.aiSessionRows()[0]).toEqual(
            expect.objectContaining({
                isExpanded: true,
                hiddenItemsCount: 1,
                visibleItems: [
                    { nameLabel: 'Apple', amountLabel: '100 GENERAL.UNITS.G' },
                    { nameLabel: 'Milk', amountLabel: '200 GENERAL.UNITS.ML' },
                    { nameLabel: 'Bread', amountLabel: '1 GENERAL.UNITS.PCS' },
                ],
            }),
        );
    });

    it('should collapse expanded session on second toggle', async () => {
        const { component } = await setupComponentAsync([createSession()]);

        component.toggleAiSessionExpanded(0);
        component.toggleAiSessionExpanded(0);

        expect(component.aiSessionRows()[0]?.isExpanded).toBe(false);
    });
});

describe('MealAiSessionsComponent actions', () => {
    it('should emit session actions with session index', async () => {
        const { component } = await setupComponentAsync([createSession()]);
        const editHandler = vi.fn();
        const deleteHandler = vi.fn();
        component.editSession.subscribe(editHandler);
        component.deleteSession.subscribe(deleteHandler);

        component.onEditSession(1);
        component.onDeleteSession(2);

        expect(editHandler).toHaveBeenCalledWith(1);
        expect(deleteHandler).toHaveBeenCalledWith(2);
    });
});

async function setupComponentAsync(
    aiSessions: ConsumptionAiSessionManageDto[],
): Promise<{ component: MealAiSessionsComponent; fixture: ComponentFixture<MealAiSessionsComponent> }> {
    await TestBed.configureTestingModule({
        imports: [MealAiSessionsComponent, TranslateModule.forRoot()],
    }).compileComponents();

    TestBed.inject(TranslateService).use('en');

    const fixture = TestBed.createComponent(MealAiSessionsComponent);
    fixture.componentRef.setInput('aiSessions', aiSessions);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createSession(): ConsumptionAiSessionManageDto {
    return {
        imageUrl: 'https://example.test/meal.jpg',
        items: [
            createAiItem({ nameEn: 'apple', amount: 100, unit: 'g', calories: 80, proteins: 1, fats: 0, carbs: 20 }),
            createAiItem({ nameEn: 'milk', amount: 200, unit: 'ml', calories: 90, proteins: 8, fats: 4, carbs: 10 }),
            createAiItem({ nameEn: 'bread', amount: 1, unit: 'pcs', calories: 40, proteins: 2, fats: 1, carbs: 5 }),
        ],
    };
}

function createAiItem(values: Partial<ConsumptionAiItemManageDto>): ConsumptionAiItemManageDto {
    return {
        nameEn: '',
        amount: 0,
        unit: '',
        calories: 0,
        proteins: 0,
        fats: 0,
        carbs: 0,
        fiber: 0,
        alcohol: 0,
        ...values,
    };
}
