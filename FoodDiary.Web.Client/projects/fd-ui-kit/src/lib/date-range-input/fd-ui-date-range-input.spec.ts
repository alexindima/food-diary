import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { disabled, form, FormField } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input';
import { FdUiDateRangeInputComponent, type FdUiDateRangeValue } from './fd-ui-date-range-input';

const START = new Date('2026-09-15T00:00:00');
const END = new Date('2026-10-01T00:00:00');
let fixture: ComponentFixture<FdUiDateRangeInputComponent>;

@Component({
    imports: [FdUiDateRangeInputComponent, FormField],
    template: '<fd-ui-date-range-input [formField]="rangeForm.range" />',
})
class RangeFormHost {
    public readonly model = signal<{ range: FdUiDateRangeValue | null; disabled: boolean }>({
        range: { start: START, end: END },
        disabled: false,
    });
    public readonly rangeForm = form(this.model, path => {
        disabled(path.range, { when: context => context.valueOf(path.disabled) });
    });
}

beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FdUiDateRangeInputComponent, RangeFormHost] }).compileComponents();
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

    it('does not emit a user change for an external range or null reset', () => {
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);

        fixture.componentRef.setInput('value', { start: END, end: null });
        fixture.detectChanges();
        expect(inputs()[0].value()).toBe('2026-10-01');

        fixture.componentRef.setInput('value', null);
        fixture.detectChanges();
        expect(inputs().map(input => input.value())).toEqual([null, null]);
        expect(fixture.componentInstance.value()).toBeNull();
        expect(changed).not.toHaveBeenCalled();
    });

    it('emits one normalized range for an endpoint edit and keeps a missing endpoint', () => {
        fixture.componentRef.setInput('value', { start: null, end: END });
        fixture.detectChanges();
        const changed = vi.fn();
        fixture.componentInstance.value.subscribe(changed);

        inputs()[1]['onDateSelect'](new Date('2024-02-29T00:00:00'));
        fixture.detectChanges();

        expect(changed).toHaveBeenCalledExactlyOnceWith({ start: null, end: new Date('2024-02-29T00:00:00') });
    });

    it('disables both endpoints through either disabled input', () => {
        fixture.componentRef.setInput('controlDisabled', true);
        fixture.detectChanges();
        expect(inputs().every(input => input.disabled())).toBe(true);

        fixture.componentRef.setInput('controlDisabled', false);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        expect(inputs().every(input => input.disabled())).toBe(true);
    });
});

describe('FdUiDateRangeInputComponent parent form', () => {
    it('updates the parent model and preserves reset, touch and disabled behavior', () => {
        const parent = TestBed.createComponent(RangeFormHost);
        parent.detectChanges();
        const child = parent.debugElement.query(By.directive(FdUiDateRangeInputComponent)).componentInstance as FdUiDateRangeInputComponent;
        const endpoints = parent.debugElement
            .queryAll(By.directive(FdUiDateInputComponent))
            .map(element => element.componentInstance as FdUiDateInputComponent);

        endpoints[0]['clearValue']();
        parent.detectChanges();
        expect(parent.componentInstance.model().range).toEqual({ start: null, end: END });
        expect(parent.componentInstance.rangeForm.range().touched()).toBe(true);

        parent.componentInstance.model.update(value => ({ ...value, disabled: true }));
        parent.detectChanges();
        expect(endpoints.every(endpoint => endpoint.disabled())).toBe(true);

        parent.componentInstance.rangeForm().reset({ range: { start: END, end: null }, disabled: false });
        parent.detectChanges();
        expect(child.touched()).toBe(false);
        expect(endpoints.map(endpoint => endpoint.value())).toEqual(['2026-10-01', null]);
    });
});
