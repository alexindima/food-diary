import { registerLocaleData } from '@angular/common';
import ru from '@angular/common/locales/ru';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { TdeeInsight } from '../../../../shared/models/tdee-insight.data';
import { TdeeInsightDialogComponent } from './tdee-insight-dialog';

const ADAPTIVE_TDEE = 2400;
const SUGGESTED_TARGET = 2200;
const FOOTER_ACTION_COUNT = 4;
registerLocaleData(ru);

describe('TDEE dialog number localization', () => {
    it('uses the selected language for totals, BMR, weight trend and suggested target, including a live language change', async () => {
        const { fixture } = await setupComponentAsync(createInsight());
        const insight = createInsight();
        const translate = TestBed.inject(TranslateService);
        const host = fixture.nativeElement as HTMLElement;
        for (const language of ['ru', 'en']) {
            translate.setTranslation(language, { TDEE_DIALOG: { SUGGESTED_TARGET: 'Target {{value}}' } }, true);
            translate.use(language);
            fixture.detectChanges();
            await fixture.whenStable();
            const integer = (value: number): string => new Intl.NumberFormat(language).format(value);
            expect(host.querySelector('.tdee-dialog__number')?.textContent).toBe(integer(ADAPTIVE_TDEE));
            expect(Array.from(host.querySelectorAll('.tdee-dialog__metric-value'), element => element.textContent.trim())).toEqual([
                `${integer(insight.estimatedTdee ?? 0)} TDEE_CARD.KCAL`,
                `${integer(ADAPTIVE_TDEE)} TDEE_CARD.KCAL`,
                `${integer(insight.bmr ?? 0)} TDEE_CARD.KCAL`,
                `${new Intl.NumberFormat(language, { minimumFractionDigits: 1 }).format(insight.weightTrendPerWeek ?? 0)} GENERAL.UNITS.KG/GENERAL.TIME.WEEK_SHORT`,
            ]);
            expect(host.querySelector('fd-tdee-insight-dialog-hint')?.textContent).toContain(`Target ${integer(SUGGESTED_TARGET)}`);
        }
    });
});

describe('TdeeInsightDialogComponent', () => {
    it('builds adaptive state and complete setup items from insight', async () => {
        const { component } = await setupComponentAsync(createInsight());

        expect(component['effectiveTdee']).toBe(ADAPTIVE_TDEE);
        expect(component['stateKey']).toBe('TDEE_DIALOG.STATE.ADAPTIVE');
        expect(component['summaryKey']).toBe('TDEE_DIALOG.SUMMARY.ADAPTIVE');
        expect(component['showSuggestion']).toBe(true);
        expect(component['setupItems'].map(item => item.complete)).toEqual([true, true, true]);
    });

    it('closes only after the suggested goal has been persisted', async () => {
        const { component, dialogRef, applyGoal } = await setupComponentAsync(createInsight());

        await component['applySuggestionAsync']();

        expect(applyGoal).toHaveBeenCalledExactlyOnceWith(SUGGESTED_TARGET);
        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(dialogRef.close).toHaveBeenCalledWith();
    });

    it('uses empty state and ignores invalid suggestion without insight', async () => {
        const { component, dialogRef } = await setupComponentAsync(null);

        await component['applySuggestionAsync']();

        expect(component['effectiveTdee']).toBeNull();
        expect(component['stateKey']).toBe('TDEE_DIALOG.STATE.EMPTY');
        expect(component['summaryKey']).toBe('TDEE_DIALOG.SUMMARY.EMPTY');
        expect(component['showSuggestion']).toBe(false);
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
});

describe('TDEE dialog application recovery', () => {
    it('places actions in the dialog footer so they remain outside the scrolling content', async () => {
        const { fixture } = await setupComponentAsync(createInsight());
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.fd-ui-dialog__footer fd-tdee-insight-dialog-footer')).not.toBeNull();
        expect(host.querySelector('.fd-ui-dialog__body fd-tdee-insight-dialog-footer')).toBeNull();
    });

    it.each([false, new Error('offline')])('retains the suggestion and exposes a retry after rejection (%s)', async failure => {
        const { component, fixture, dialogRef, applyGoal } = await setupComponentAsync(createInsight());
        if (failure instanceof Error) {
            applyGoal.mockRejectedValueOnce(failure);
        } else {
            applyGoal.mockResolvedValueOnce(failure);
        }
        await component['applySuggestionAsync']();
        fixture.detectChanges();
        expect(component['applyFailed']()).toBe(true);
        expect(component['isApplying']()).toBe(false);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();
        expect(dialogRef.close).not.toHaveBeenCalled();
        await component['applySuggestionAsync']();
        expect(component['applyFailed']()).toBe(false);
        expect(dialogRef.close).toHaveBeenCalledOnce();
    });

    it('blocks duplicate application, dismissal and other actions until the pending save resolves', async () => {
        const { component, fixture, dialogRef, applyGoal } = await setupComponentAsync(createInsight());
        const pending = new Subject<boolean>();
        applyGoal.mockReturnValueOnce(firstValueFrom(pending));
        const saving = component['applySuggestionAsync']();
        fixture.detectChanges();
        expect(dialogRef.disableClose).toBe(true);
        const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('fd-tdee-insight-dialog-footer button');
        expect(buttons).toHaveLength(FOOTER_ACTION_COUNT);
        expect(Array.from(buttons).every(button => button.disabled)).toBe(true);
        component['close']({ type: 'profile' });
        await component['applySuggestionAsync']();
        expect(applyGoal).toHaveBeenCalledOnce();
        expect(dialogRef.close).not.toHaveBeenCalled();
        pending.next(true);
        pending.complete();
        await saving;
        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(dialogRef.disableClose).toBe(false);
    });
});

async function setupComponentAsync(insight: TdeeInsight | null): Promise<{
    component: TdeeInsightDialogComponent;
    fixture: ComponentFixture<TdeeInsightDialogComponent>;
    dialogRef: { close: ReturnType<typeof vi.fn>; disableClose: boolean | undefined };
    applyGoal: ReturnType<typeof vi.fn<(target: number) => Promise<boolean>>>;
}> {
    const dialogRef = { close: vi.fn(), disableClose: false };
    const applyGoal = vi.fn<(target: number) => Promise<boolean>>().mockResolvedValue(true);

    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [TdeeInsightDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: insight === null ? null : { insight, applyGoalAsync: applyGoal } },
            ],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(TdeeInsightDialogComponent);

    return {
        component: fixture.componentInstance,
        fixture,
        dialogRef,
        applyGoal,
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
