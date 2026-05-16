import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import type { TdeeInsight } from '../../models/tdee-insight.data';
import { TdeeInsightDialogComponent } from './tdee-insight-dialog.component';

const ADAPTIVE_TDEE = 2400;
const SUGGESTED_TARGET = 2200;

describe('TdeeInsightDialogComponent', () => {
    it('builds adaptive state and complete setup items from insight', async () => {
        const { component } = await setupComponentAsync(createInsight());

        expect(component.effectiveTdee).toBe(ADAPTIVE_TDEE);
        expect(component.stateKey).toBe('TDEE_DIALOG.STATE.ADAPTIVE');
        expect(component.summaryKey).toBe('TDEE_DIALOG.SUMMARY.ADAPTIVE');
        expect(component.showSuggestion).toBe(true);
        expect(component.setupItems.map(item => item.complete)).toEqual([true, true, true]);
    });

    it('closes with applyGoal action only when suggestion is valid', async () => {
        const { component, dialogRef } = await setupComponentAsync(createInsight());

        component.applySuggestion();

        expect(dialogRef.close).toHaveBeenCalledWith({ type: 'applyGoal', target: SUGGESTED_TARGET });
    });

    it('uses empty state and ignores invalid suggestion without insight', async () => {
        const { component, dialogRef } = await setupComponentAsync(null);

        component.applySuggestion();

        expect(component.effectiveTdee).toBeNull();
        expect(component.stateKey).toBe('TDEE_DIALOG.STATE.EMPTY');
        expect(component.summaryKey).toBe('TDEE_DIALOG.SUMMARY.EMPTY');
        expect(component.showSuggestion).toBe(false);
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
});

async function setupComponentAsync(insight: TdeeInsight | null): Promise<{
    component: TdeeInsightDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
}> {
    const dialogRef = { close: vi.fn() };

    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [TdeeInsightDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: insight === null ? null : { insight } },
            ],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(TdeeInsightDialogComponent);

    return {
        component: fixture.componentInstance,
        dialogRef,
    };
}

function createInsight(): TdeeInsight {
    return {
        estimatedTdee: 2300,
        adaptiveTdee: ADAPTIVE_TDEE,
        bmr: 1700,
        suggestedCalorieTarget: SUGGESTED_TARGET,
        currentCalorieTarget: 2000,
        weightTrendPerWeek: -0.3,
        confidence: 'high',
        dataDaysUsed: 21,
        goalAdjustmentHint: 'decrease',
    };
}
