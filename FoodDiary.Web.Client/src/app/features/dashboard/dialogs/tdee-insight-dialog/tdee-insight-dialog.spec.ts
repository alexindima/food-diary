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
const SETUP_ITEM_COUNT = 3;
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

    it('closes only after the user applies the suggestion and the goal has been persisted', async () => {
        const { fixture, dialogRef, applyGoal } = await setupComponentAsync(createInsight());
        const save = new Subject<boolean>();
        applyGoal.mockReturnValueOnce(firstValueFrom(save));
        fixture.detectChanges();
        const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('fd-tdee-insight-dialog-footer button');
        const apply = Array.from(buttons).find(button => button.textContent.trim().endsWith('TDEE_CARD.APPLY'));
        expect(apply).toBeDefined();

        apply?.click();
        expect(applyGoal).toHaveBeenCalledExactlyOnceWith(SUGGESTED_TARGET);
        expect(dialogRef.close).not.toHaveBeenCalled();

        save.next(true);
        save.complete();
        await fixture.whenStable();
        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(dialogRef.close).toHaveBeenCalledWith();
    });

    it('uses empty state and ignores invalid suggestion without insight', async () => {
        const { component, fixture, dialogRef, applyGoal } = await setupComponentAsync(null);
        fixture.detectChanges();
        expect(footerButton(fixture, 'TDEE_CARD.APPLY')).toBeUndefined();
        expect(applyGoal).not.toHaveBeenCalled();

        expect(component['effectiveTdee']).toBeNull();
        expect(component['stateKey']).toBe('TDEE_DIALOG.STATE.EMPTY');
        expect(component['summaryKey']).toBe('TDEE_DIALOG.SUMMARY.EMPTY');
        expect(component['showSuggestion']).toBe(false);
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
});

describe('TDEE dialog estimate and setup status', () => {
    const profileOnly: TdeeInsight = {
        ...createInsight(),
        adaptiveTdee: null,
        suggestedCalorieTarget: null,
        dataDaysUsed: 0,
        weightTrendPerWeek: null,
        confidence: 'none',
        goalAdjustmentHint: null,
    };
    it.each([
        { scenario: 'missing insight', insight: null, state: 'EMPTY', completed: [] },
        { scenario: 'profile estimate', insight: profileOnly, state: 'ESTIMATED', completed: ['PROFILE'] },
        {
            scenario: 'measured stability',
            insight: { ...profileOnly, weightTrendPerWeek: 0 },
            state: 'ESTIMATED',
            completed: ['PROFILE', 'WEIGHT'],
        },
    ])('shows the evidence actually available for $scenario', async ({ insight, state, completed }) => {
        const { fixture } = await setupComponentAsync(insight, true);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.tdee-dialog__state')?.textContent.trim()).toBe(`TDEE_DIALOG.STATE.${state}`);
        expect(host.querySelector('.tdee-dialog__summary-text')?.textContent.trim()).toBe(`TDEE_DIALOG.SUMMARY.${state}`);
        expect(host.querySelectorAll('.tdee-dialog__setup-item')).toHaveLength(SETUP_ITEM_COUNT);
        const completedTitles = [...host.querySelectorAll('.tdee-dialog__setup-item--complete .tdee-dialog__setup-title')].map(item =>
            item.textContent.trim(),
        );
        expect(completedTitles).toEqual(completed.map(key => `TDEE_DIALOG.SETUP.${key}_TITLE`));
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
        fixture.detectChanges();
        footerButton(fixture, 'TDEE_CARD.APPLY')?.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(component['applyFailed']()).toBe(true);
        expect(component['isApplying']()).toBe(false);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();
        expect(dialogRef.close).not.toHaveBeenCalled();
        footerButton(fixture, 'TDEE_CARD.APPLY')?.click();
        await fixture.whenStable();
        expect(applyGoal).toHaveBeenCalledTimes(2);
        expect(component['applyFailed']()).toBe(false);
        expect(dialogRef.close).toHaveBeenCalledOnce();
    });

    it('blocks duplicate application, dismissal and other actions until the pending save resolves', async () => {
        const { fixture, dialogRef, applyGoal } = await setupComponentAsync(createInsight());
        const pending = new Subject<boolean>();
        applyGoal.mockReturnValueOnce(firstValueFrom(pending));
        fixture.detectChanges();
        const apply = footerButton(fixture, 'TDEE_CARD.APPLY');
        apply?.click();
        apply?.click();
        footerButton(fixture, 'TDEE_DIALOG.ACTIONS.PROFILE')?.click();
        fixture.detectChanges();
        expect(dialogRef.disableClose).toBe(true);
        const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('fd-tdee-insight-dialog-footer button');
        expect(buttons).toHaveLength(FOOTER_ACTION_COUNT);
        expect(Array.from(buttons).every(button => button.disabled)).toBe(true);
        expect(applyGoal).toHaveBeenCalledExactlyOnceWith(SUGGESTED_TARGET);
        expect(dialogRef.close).not.toHaveBeenCalled();
        pending.next(true);
        pending.complete();
        await fixture.whenStable();
        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(dialogRef.disableClose).toBe(false);
    });
});

describe('TDEE dialog footer actions', () => {
    it.each(['profile', 'weight', 'meal', 'goals'] as const)('returns the selected %s action from the rendered footer', async type => {
        const { fixture, dialogRef, applyGoal } = await setupComponentAsync(type === 'goals' ? null : createInsight());
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const label = `TDEE_DIALOG.ACTIONS.${type.toUpperCase()}`;
        const action = Array.from(host.querySelectorAll<HTMLButtonElement>('fd-tdee-insight-dialog-footer button')).find(button =>
            button.textContent.trim().endsWith(label),
        );
        expect(action, `Missing ${type} continuation`).toBeDefined();

        action?.click();

        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith({ type });
        expect(applyGoal).not.toHaveBeenCalled();
    });

    it.each([null, 0])('offers manual goal settings when the suggested target is %s', async suggestedCalorieTarget => {
        const { fixture, dialogRef, applyGoal } = await setupComponentAsync({ ...createInsight(), suggestedCalorieTarget });
        fixture.detectChanges();
        const buttons = Array.from(
            (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('fd-tdee-insight-dialog-footer button'),
        );
        expect(buttons.some(button => button.textContent.trim().endsWith('TDEE_CARD.APPLY'))).toBe(false);
        const goalSettings = buttons.find(button => button.textContent.trim().endsWith('TDEE_DIALOG.ACTIONS.GOALS'));
        expect(goalSettings).toBeDefined();

        goalSettings?.click();

        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith({ type: 'goals' });
        expect(applyGoal).not.toHaveBeenCalled();
    });
});

function footerButton(fixture: ComponentFixture<TdeeInsightDialogComponent>, label: string): HTMLButtonElement | undefined {
    const host = fixture.nativeElement as HTMLElement;
    return Array.from(host.querySelectorAll<HTMLButtonElement>('fd-tdee-insight-dialog-footer button')).find(button =>
        button.textContent.trim().endsWith(label),
    );
}

async function setupComponentAsync(
    insight: TdeeInsight | null,
    withDialogData = insight !== null,
): Promise<{
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
                { provide: FD_UI_DIALOG_DATA, useValue: withDialogData ? { insight, applyGoalAsync: applyGoal } : null },
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
