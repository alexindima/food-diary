import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom, Subject } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { WeeklyGoal } from '../../models/weekly-goal.data';
import { WeeklyGoalDialogComponent, type WeeklyGoalDialogData } from './weekly-goal-dialog';

function setup(disableClose = false): {
    fixture: ComponentFixture<WeeklyGoalDialogComponent>;
    component: WeeklyGoalDialogComponent;
    pending: Subject<WeeklyGoal | null>;
    saveGoalAsync: Mock<WeeklyGoalDialogData['saveGoalAsync']>;
    dialogRef: { disableClose: boolean; close: Mock };
} {
    const pending = new Subject<WeeklyGoal | null>();
    const saveGoalAsync = vi.fn().mockReturnValueOnce(firstValueFrom(pending)).mockResolvedValue(null);
    const dialogRef = { disableClose, close: vi.fn() };
    TestBed.configureTestingModule({
        imports: [WeeklyGoalDialogComponent],
        providers: [
            ...provideTranslateTesting(),
            { provide: FdUiDialogRef, useValue: dialogRef },
            {
                provide: FD_UI_DIALOG_DATA,
                useValue: { weekStart: '2026-09-28', titleKey: 'COMMON.SAVE', goal: null, saveGoalAsync },
            },
        ],
    });
    const fixture = TestBed.createComponent(WeeklyGoalDialogComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, pending, saveGoalAsync, dialogRef };
}

describe('Weekly goal save recovery', () => {
    it('keeps a failed save visible, preserves the draft and allows retry', async () => {
        const { fixture, component, pending, saveGoalAsync, dialogRef } = setup();
        component['selectedTarget'].set('3');
        component['reminderEnabled'].set(true);
        component['reminderTime'].set('19:45');
        expect((fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__close-button')).not.toBeNull();
        const saving = component['saveAsync']();
        fixture.detectChanges();
        expect(dialogRef.disableClose).toBe(true);
        expect((fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__close-button')).toBeNull();
        await component['saveAsync']();
        expect(saveGoalAsync).toHaveBeenCalledOnce();
        pending.error(new Error('offline'));
        await saving;
        fixture.detectChanges();
        expect(dialogRef.disableClose).toBe(false);
        expect(dialogRef.close).not.toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__close-button')).not.toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();
        expect(component['selectedTarget']()).toBe('3');
        expect(component['reminderTime']()).toBe('19:45');
        await component['saveAsync']();
        expect(saveGoalAsync).toHaveBeenLastCalledWith(expect.objectContaining({ targetDays: 3, reminderTime: '19:45' }));
        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(dialogRef.close).toHaveBeenCalledWith(null);
        expect(component['saveFailed']()).toBe(false);
    });

    it.each([false, true])('restores the original dismissal policy after success (%s)', async disableClose => {
        const { component, pending, dialogRef } = setup(disableClose);
        const saving = component['saveAsync']();
        expect(dialogRef.disableClose).toBe(true);
        expect(dialogRef.close).not.toHaveBeenCalled();
        pending.next(null);
        pending.complete();
        await saving;
        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(dialogRef.close).toHaveBeenCalledWith(null);
        expect(dialogRef.disableClose).toBe(disableClose);
    });
});
