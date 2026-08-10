import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WeeklyReviewDialogComponent, type WeeklyReviewDialogData } from './weekly-review-dialog';

const EXPECTED_INSIGHT_COUNT = 3;

describe('WeeklyReviewDialogComponent', () => {
    it('renders every insight and the selected week metrics', async () => {
        const data: WeeklyReviewDialogData = {
            review: {
                daysLogged: 2,
                hasEnoughData: false,
                summaryKey: 'WEEKLY_CHECK_IN.SUMMARY.LIMITED',
                focusTitleKey: 'WEEKLY_CHECK_IN.FOCUS.LOGGING_TITLE',
                focusDescriptionKey: 'WEEKLY_CHECK_IN.FOCUS.LOGGING_DESCRIPTION',
                focusTarget: 5,
                insights: [
                    { key: 'hydration', icon: 'water_drop', tone: 'positive', labelKey: 'WEEKLY_CHECK_IN.INSIGHTS.HYDRATION_IMPROVED' },
                    { key: 'protein', icon: 'fitness_center', tone: 'attention', labelKey: 'WEEKLY_CHECK_IN.INSIGHTS.PROTEIN_LOW' },
                    { key: 'logging', icon: 'event_available', tone: 'info', labelKey: 'WEEKLY_CHECK_IN.INSIGHTS.LOGGING_PROGRESS_FEW' },
                ],
            },
            week: {
                totalCalories: 1086,
                avgDailyCalories: 543,
                avgProteins: 20.2,
                avgFats: 18,
                avgCarbs: 64,
                mealsLogged: 4,
                daysLogged: 2,
                weightStart: null,
                weightEnd: null,
                waistStart: null,
                waistEnd: null,
                totalHydrationMl: 3000,
                avgDailyHydrationMl: 1500,
            },
        };

        await TestBed.configureTestingModule({
            imports: [WeeklyReviewDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FD_UI_DIALOG_DATA, useValue: data },
                { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(WeeklyReviewDialogComponent);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelectorAll('.weekly-review-dialog__insight')).toHaveLength(EXPECTED_INSIGHT_COUNT);
        expect(root.textContent).toContain('543');
        expect(root.textContent).toContain('20.2');
        expect(root.textContent).toContain('1,500');
    });
});
