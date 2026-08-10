import { OverlayContainer } from '@angular/cdk/overlay';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiWeekPickerComponent } from './fd-ui-week-picker';

const TEST_YEAR = 2026;
const AUGUST_INDEX = 7;
const CURRENT_WEEK_DAY = 10;
const PREVIOUS_WEEK_DAY = 3;
const NEXT_WEEK_DAY = 17;

describe('FdUiWeekPickerComponent', () => {
    let fixture: ComponentFixture<FdUiWeekPickerComponent>;
    let component: FdUiWeekPickerComponent;
    let overlayContainer: OverlayContainer;
    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiWeekPickerComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        fixture = TestBed.createComponent(FdUiWeekPickerComponent);
        component = fixture.componentInstance;
        overlayContainer = TestBed.inject(OverlayContainer);
        component.value.set(new Date(TEST_YEAR, AUGUST_INDEX, CURRENT_WEEK_DAY));
        fixture.componentRef.setInput('min', new Date(TEST_YEAR, AUGUST_INDEX, PREVIOUS_WEEK_DAY));
        fixture.componentRef.setInput('max', new Date(TEST_YEAR, AUGUST_INDEX, CURRENT_WEEK_DAY));
        fixture.detectChanges();
    });

    it('normalizes values and navigates between weeks inside min and max', () => {
        component['selectPreviousWeek']();
        expect(component.value().getDate()).toBe(PREVIOUS_WEEK_DAY);

        component['selectNextWeek']();
        expect(component.value().getDate()).toBe(CURRENT_WEEK_DAY);

        component['selectNextWeek']();
        expect(component.value().getDate()).not.toBe(NEXT_WEEK_DAY);
    });

    it('disables navigation controls at range boundaries', () => {
        const buttons = host().querySelectorAll<HTMLButtonElement>('button');
        expect(buttons[0].disabled).toBe(false);
        expect(buttons[2].disabled).toBe(true);
    });

    it('opens the reusable week calendar and closes after selection', () => {
        component['open']();
        fixture.detectChanges();

        expect(overlayContainer.getContainerElement().querySelector('.fd-ui-week-picker__panel')).not.toBeNull();

        component['onCalendarSelect'](new Date(TEST_YEAR, AUGUST_INDEX, PREVIOUS_WEEK_DAY));
        fixture.detectChanges();

        expect(component.value().getDate()).toBe(PREVIOUS_WEEK_DAY);
        expect(overlayContainer.getContainerElement().querySelector('.fd-ui-week-picker__panel')).toBeNull();
    });
});
