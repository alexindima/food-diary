import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { MealAiItemManageDto, MealAiSessionManageDto } from '../../../../../shared/models/meal.data';
import { MealAiSessionsComponent } from './meal-ai-sessions';

describe('MealAiSessionsComponent rows', () => {
    it('should build collapsed AI session rows with preview items and totals', async () => {
        const { component } = await setupComponentAsync([createSession()]);

        expect(component['aiSessionRows']()).toEqual([
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

        component['toggleAiSessionExpanded'](0);

        expect(component['aiSessionRows']()[0]).toEqual(
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

        component['toggleAiSessionExpanded'](0);
        component['toggleAiSessionExpanded'](0);

        expect(component['aiSessionRows']()[0]?.isExpanded).toBe(false);
    });
});

describe('MealAiSessionsComponent actions', () => {
    it('should emit session actions with session index', async () => {
        const { component } = await setupComponentAsync([createSession()]);
        const editHandler = vi.fn();
        const deleteHandler = vi.fn();
        component['editSession'].subscribe(editHandler);
        component['deleteSession'].subscribe(deleteHandler);

        component['onEditSession'](1);
        component['onDeleteSession'](2);

        expect(editHandler).toHaveBeenCalledWith(1);
        expect(deleteHandler).toHaveBeenCalledWith(2);
    });
});

describe('Meal photo summary after deleting a recognition session', () => {
    it.each([
        { expandedRow: 0, removedRow: 0, remainingExpanded: false, survivor: 'Pasta', extraIngredient: 'Basil' },
        { expandedRow: 1, removedRow: 0, remainingExpanded: true, survivor: 'Pasta', extraIngredient: 'Basil' },
        { expandedRow: 0, removedRow: 1, remainingExpanded: true, survivor: 'Apple', extraIngredient: 'Bread' },
    ])(
        'keeps expansion on its original session after deleting row $removedRow with row $expandedRow open',
        async ({ expandedRow, removedRow, remainingExpanded, survivor, extraIngredient }) => {
            const remaining = {
                ...createSession(),
                items: [
                    createAiItem({ nameEn: 'pasta', amount: 1.5, unit: 'g' }),
                    createAiItem({ nameEn: 'sauce', amount: 12, unit: 'ml' }),
                    createAiItem({ nameEn: 'basil', amount: 2, unit: 'g' }),
                ],
            };
            const first = createSession();
            first.items[0] = { ...first.items[0], amount: 1.5 };
            const sessions = [first, remaining];
            const { component, fixture } = await setupComponentAsync(sessions);
            const host = fixture.nativeElement as HTMLElement;
            const rowSelector = '.meal-manage__ai-summary';
            const toggleSelector = '.meal-manage__ai-summary-more button';
            component.deleteSession.subscribe(index => {
                fixture.componentRef.setInput(
                    'aiSessions',
                    sessions.filter((_, rowIndex) => rowIndex !== index),
                );
            });

            const rows = host.querySelectorAll<HTMLElement>(rowSelector);
            summaryButton(rows[expandedRow], toggleSelector).click();
            fixture.detectChanges();
            expect(summaryButton(rows[expandedRow], toggleSelector).getAttribute('aria-expanded')).toBe('true');

            summaryButton(rows[removedRow], 'button[aria-label="MEAL_MANAGE.ITEMS_AI_DELETE"]').click();
            fixture.detectChanges();
            const currentRows = host.querySelectorAll<HTMLElement>(rowSelector);
            expect(currentRows).toHaveLength(1);
            const surviving = currentRows[0];
            expect(surviving.textContent).toContain(survivor);
            expect(summaryButton(surviving, toggleSelector).getAttribute('aria-expanded')).toBe(String(remainingExpanded));
            expect(surviving.textContent.includes(extraIngredient)).toBe(remainingExpanded);
            expect(surviving.textContent).toContain('1.5 GENERAL.UNITS.G');

            await firstValueFrom(TestBed.inject(TranslateService).use('ru'));
            fixture.detectChanges();
            expect(surviving.textContent).toContain('1,5 GENERAL.UNITS.G');
            expect(summaryButton(surviving, toggleSelector).getAttribute('aria-expanded')).toBe(String(remainingExpanded));
        },
    );
});

function summaryButton(row: HTMLElement, selector: string): HTMLButtonElement {
    const button = row.querySelector<HTMLButtonElement>(selector);
    if (button === null) {
        throw new Error('Expected a meal photo summary action');
    }
    return button;
}

async function setupComponentAsync(
    aiSessions: MealAiSessionManageDto[],
): Promise<{ component: MealAiSessionsComponent; fixture: ComponentFixture<MealAiSessionsComponent> }> {
    await TestBed.configureTestingModule({
        imports: [MealAiSessionsComponent],
        providers: [provideTranslateTesting()],
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

function createSession(): MealAiSessionManageDto {
    return {
        imageUrl: 'https://example.test/meal.jpg',
        items: [
            createAiItem({ nameEn: 'apple', amount: 100, unit: 'g', calories: 80, proteins: 1, fats: 0, carbs: 20 }),
            createAiItem({ nameEn: 'milk', amount: 200, unit: 'ml', calories: 90, proteins: 8, fats: 4, carbs: 10 }),
            createAiItem({ nameEn: 'bread', amount: 1, unit: 'pcs', calories: 40, proteins: 2, fats: 1, carbs: 5 }),
        ],
    };
}

function createAiItem(values: Partial<MealAiItemManageDto>): MealAiItemManageDto {
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
