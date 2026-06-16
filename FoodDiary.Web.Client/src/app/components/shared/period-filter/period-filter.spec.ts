import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import type { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { PeriodFilterComponent } from './period-filter';

const NON_STRING_RANGE_VALUE = 42;

@Component({
    imports: [PeriodFilterComponent],
    template: `
        <fd-period-filter
            [tabs]="tabs"
            [selectedValue]="selectedValue()"
            [rangeField]="rangeForm.range"
            [displayRange]="displayRange()"
            (rangeChange)="onRangeChange($event)"
        />
    `,
})
class TestHostComponent {
    public readonly tabs: FdUiTab[] = [
        { value: 'week', label: 'Week' },
        { value: 'month', label: 'Month' },
        { value: 'custom', label: 'Custom' },
    ];

    public readonly selectedValue = signal('week');
    public readonly rangeModel = signal<{ range: { start: Date | null; end: Date | null } | null }>({ range: null });
    public readonly displayRange = signal<{ start: Date; end: Date } | null>(null);
    public readonly rangeForm = form(this.rangeModel);
    public lastEmittedValue: string | null = null;

    public onRangeChange(value: string): void {
        this.lastEmittedValue = value;
    }
}

describe('PeriodFilterComponent', () => {
    let hostFixture: ComponentFixture<TestHostComponent>;
    let host: TestHostComponent;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();

        hostFixture = TestBed.createComponent(TestHostComponent);
        host = hostFixture.componentInstance;
        hostFixture.detectChanges();
    });

    it('should create', () => {
        const periodFilter = hostFixture.debugElement.children[0].componentInstance as PeriodFilterComponent;
        expect(periodFilter).toBeTruthy();
    });

    it('should handle tab selection', () => {
        const periodFilter = hostFixture.debugElement.children[0].componentInstance as PeriodFilterComponent;

        periodFilter['onRangeChange']('month');

        expect(host.lastEmittedValue).toBe('month');
    });

    it('should emit filter changes', () => {
        const periodFilter = hostFixture.debugElement.children[0].componentInstance as PeriodFilterComponent;

        periodFilter['onRangeChange']('custom');

        expect(host.lastEmittedValue).toBe('custom');
    });

    it('should not emit for non-string values', () => {
        const periodFilter = hostFixture.debugElement.children[0].componentInstance as PeriodFilterComponent;

        periodFilter['onRangeChange'](NON_STRING_RANGE_VALUE);

        expect(host.lastEmittedValue).toBeNull();
    });

    it('should sync display range when not custom', () => {
        const start = new Date('2026-06-01T00:00:00Z');
        const end = new Date('2026-06-16T00:00:00Z');

        host.selectedValue.set('week');
        host.displayRange.set({ start, end });
        hostFixture.detectChanges();

        expect(host.rangeModel().range).toEqual({ start, end });
    });

    it('should not rewrite equivalent display range values', () => {
        const start = new Date('2026-06-01T00:00:00Z');
        const end = new Date('2026-06-16T00:00:00Z');

        host.selectedValue.set('week');
        host.displayRange.set({ start, end });
        hostFixture.detectChanges();
        const syncedRange = host.rangeModel().range;

        host.displayRange.set({ start: new Date(start), end: new Date(end) });
        hostFixture.detectChanges();

        expect(host.rangeModel().range).toBe(syncedRange);
    });

    it('should keep range field available when custom is selected', () => {
        host.selectedValue.set('custom');
        hostFixture.detectChanges();

        expect(host.rangeForm.range().disabled()).toBe(false);
    });
});
