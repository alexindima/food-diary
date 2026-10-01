import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input';
import { FdUiDateRangeInputComponent } from './fd-ui-date-range-input';

const START = new Date('2026-09-15T00:00:00');
const END = new Date('2026-10-01T00:00:00');
let fixture: ComponentFixture<FdUiDateRangeInputComponent>;

beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FdUiDateRangeInputComponent] }).compileComponents();
    fixture = TestBed.createComponent(FdUiDateRangeInputComponent);
    fixture.componentRef.setInput('value', { start: START, end: END });
    fixture.detectChanges();
});

function inputs(): FdUiDateInputComponent[] {
    return fixture.debugElement
        .queryAll(By.directive(FdUiDateInputComponent))
        .map(element => element.componentInstance as FdUiDateInputComponent);
}

describe('FdUiDateRangeInputComponent calendar boundaries', () => {
    it('bounds both endpoints and permits the same date', () => {
        const [start, end] = inputs();
        expect(start.latestDate()).toBe('2026-10-01');
        expect(end.earliestDate()).toBe('2026-09-15');
        fixture.componentRef.setInput('value', { start: START, end: START });
        fixture.detectChanges();
        expect(start.latestDate()).toBe('2026-09-15');
        expect(end.earliestDate()).toBe('2026-09-15');
    });

    it('updates the opposite boundary after a user changes an endpoint', () => {
        const [start, end] = inputs();
        end['onDateSelect'](new Date('2026-09-20T00:00:00'));
        fixture.detectChanges();
        expect(start.latestDate()).toBe('2026-09-20');
        expect(fixture.componentInstance.value()?.end).toEqual(new Date('2026-09-20T00:00:00'));
        start['onDateSelect'](new Date('2026-09-18T00:00:00'));
        fixture.detectChanges();
        expect(end.earliestDate()).toBe('2026-09-18');
    });

    it('leaves the other calendar unrestricted when an endpoint is missing', () => {
        fixture.componentRef.setInput('value', { start: null, end: END });
        fixture.detectChanges();
        const [start, end] = inputs();
        expect(end.earliestDate()).toBeUndefined();
        expect(start.latestDate()).toBe('2026-10-01');
        fixture.componentRef.setInput('value', { start: START, end: null });
        fixture.detectChanges();
        expect(start.latestDate()).toBeUndefined();
        expect(end.earliestDate()).toBe('2026-09-15');
    });
});
