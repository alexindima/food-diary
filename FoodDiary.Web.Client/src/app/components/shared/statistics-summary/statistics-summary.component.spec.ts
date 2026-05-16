import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { StatisticsSummaryComponent, type StatisticsSummaryExportFormat } from './statistics-summary.component';

async function setupStatisticsSummaryAsync(): Promise<ComponentFixture<StatisticsSummaryComponent>> {
    await TestBed.configureTestingModule({
        imports: [StatisticsSummaryComponent, TranslateModule.forRoot()],
        providers: [provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(StatisticsSummaryComponent);
    fixture.componentRef.setInput('summary', null);
    fixture.componentRef.setInput('summarySparklineData', null);
    fixture.componentRef.setInput('summarySparklineOptions', null);
    fixture.componentRef.setInput('macroSparklineData', null);
    fixture.componentRef.setInput('exportingFormat', null);
    return fixture;
}

describe('StatisticsSummaryComponent', () => {
    it('emits requested export format', async () => {
        const fixture = await setupStatisticsSummaryAsync();
        const component = fixture.componentInstance;
        const exportSpy = vi.fn<(format: StatisticsSummaryExportFormat) => void>();
        component.exportRequested.subscribe(format => {
            exportSpy(format);
        });
        fixture.detectChanges();

        component.export('csv');

        expect(exportSpy).toHaveBeenCalledWith('csv');
    });
});
