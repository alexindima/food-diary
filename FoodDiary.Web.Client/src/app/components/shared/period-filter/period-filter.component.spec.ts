import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { type FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { beforeEach, describe, expect, it } from 'vitest';

import { PeriodFilterComponent } from './period-filter.component';

@Component({
    standalone: true,
    imports: [PeriodFilterComponent],
    template: `
        <fd-period-filter
            [tabs]="tabs"
            [selectedValue]="selectedValue()"
            [rangeControl]="rangeControl"
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
    public readonly rangeControl = new FormControl<{ start: Date | null; end: Date | null } | null>(null);
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
            imports: [TestHostComponent, TranslateModule.forRoot()],
            providers: [provideNoopAnimations()],
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

        periodFilter.onRangeChange('month');

        expect(host.lastEmittedValue).toBe('month');
    });

    it('should emit filter changes', () => {
        const periodFilter = hostFixture.debugElement.children[0].componentInstance as PeriodFilterComponent;

        periodFilter.onRangeChange('custom');

        expect(host.lastEmittedValue).toBe('custom');
    });

    it('should not emit for non-string values', () => {
        const periodFilter = hostFixture.debugElement.children[0].componentInstance as PeriodFilterComponent;

        periodFilter.onRangeChange(42 as unknown as string);

        expect(host.lastEmittedValue).toBeNull();
    });

    it('should disable range control when not custom', () => {
        host.selectedValue.set('week');
        hostFixture.detectChanges();

        expect(host.rangeControl.disabled).toBe(true);
    });

    it('should enable range control when custom is selected', () => {
        host.selectedValue.set('custom');
        hostFixture.detectChanges();

        expect(host.rangeControl.enabled).toBe(true);
    });
});
