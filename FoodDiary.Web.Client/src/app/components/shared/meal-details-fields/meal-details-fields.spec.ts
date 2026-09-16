import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { DEFAULT_SATIETY_LEVEL } from '../../../shared/lib/satiety-level.utils';
import { MealDetailsFieldsComponent } from './meal-details-fields';

const INVALID_SATIETY_LEVEL = 99;
const MAX_SATIETY_LEVEL = 5;

async function setupMealDetailsFieldsAsync(): Promise<ComponentFixture<MealDetailsFieldsComponent>> {
    await TestBed.configureTestingModule({
        imports: [MealDetailsFieldsComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealDetailsFieldsComponent);
    fixture.componentRef.setInput('date', '2026-05-17');
    fixture.componentRef.setInput('time', '12:30');
    fixture.componentRef.setInput('comment', '');
    return fixture;
}

describe('MealDetailsFieldsComponent satiety', () => {
    it('emits edited dates and accepts subsequent parent updates without echoing them', async () => {
        const fixture = await setupMealDetailsFieldsAsync();
        const changes: string[] = [];
        fixture.componentInstance.date.subscribe(value => changes.push(value));
        fixture.detectChanges();

        const dateInput = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('#meal-details-date');
        if (dateInput === null) {
            throw new Error('Date input was not rendered');
        }
        dateInput.value = '2026-05-18';
        dateInput.dispatchEvent(new Event('input'));
        fixture.detectChanges();
        expect(fixture.componentInstance.date()).toBe('2026-05-18');
        expect(changes).toEqual(['2026-05-18']);

        fixture.componentRef.setInput('date', '2026-05-19');
        fixture.detectChanges();
        expect(dateInput.value).toBe('2026-05-19');
        expect(changes).toEqual(['2026-05-18']);
    });

    it('normalizes invalid satiety values to default', async () => {
        const fixture = await setupMealDetailsFieldsAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component['onPreMealSatietyLevelChange'](null);
        component['onPostMealSatietyLevelChange'](INVALID_SATIETY_LEVEL);

        expect(component['preMealSatietyLevel']()).toBe(DEFAULT_SATIETY_LEVEL);
        expect(component['postMealSatietyLevel']()).toBe(MAX_SATIETY_LEVEL);
    });
});
