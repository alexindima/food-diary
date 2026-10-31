import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiDateInputComponent, FdUiTimeInputComponent } from 'fd-ui-kit';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
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
    it('offers the shared calendar and time picker when editing a meal draft', async () => {
        const fixture = await setupMealDetailsFieldsAsync();
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('fd-ui-date-input'), 'Drafts must use the shared calendar').not.toBeNull();
        expect(host.querySelector('fd-ui-time-input'), 'Drafts must use the shared time picker').not.toBeNull();
    });

    it('emits edited dates and accepts subsequent parent updates without echoing them', async () => {
        const fixture = await setupMealDetailsFieldsAsync();
        const changes: string[] = [];
        fixture.componentInstance.date.subscribe(value => changes.push(value));
        fixture.detectChanges();

        const datePicker = fixture.debugElement.query(By.directive(FdUiDateInputComponent)).componentInstance as FdUiDateInputComponent;
        datePicker.value.set('2026-05-18');
        fixture.detectChanges();
        expect(fixture.componentInstance.date()).toBe('2026-05-18');
        expect(changes).toEqual(['2026-05-18']);

        fixture.componentRef.setInput('date', '2026-05-19');
        fixture.detectChanges();
        expect(datePicker.value()).toBe('2026-05-19');
        expect(changes).toEqual(['2026-05-18']);
    });

    it('keeps unanswered ratings empty and normalizes legacy ratings', async () => {
        const fixture = await setupMealDetailsFieldsAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component['onPreMealSatietyLevelChange'](null);
        component['onPostMealSatietyLevelChange'](INVALID_SATIETY_LEVEL);

        expect(component['preMealSatietyLevel']()).toBeNull();
        expect(component['postMealSatietyLevel']()).toBe(MAX_SATIETY_LEVEL);
    });

    it('preserves calendar dates and local times from the shared pickers', async () => {
        const fixture = await setupMealDetailsFieldsAsync();
        fixture.detectChanges();
        const datePicker = fixture.debugElement.query(By.directive(FdUiDateInputComponent)).componentInstance as FdUiDateInputComponent;
        const timePicker = fixture.debugElement.query(By.directive(FdUiTimeInputComponent)).componentInstance as FdUiTimeInputComponent;
        datePicker.value.set('2024-02-29');
        timePicker.value.set('23:55');
        fixture.detectChanges();
        expect(fixture.componentInstance.date()).toBe('2024-02-29');
        expect(fixture.componentInstance.time()).toBe('23:55');
        timePicker.value.set(null);
        fixture.detectChanges();
        expect(fixture.componentInstance.time()).toBe('');
        expect(fixture.componentInstance.date()).toBe('2024-02-29');
    });
});
