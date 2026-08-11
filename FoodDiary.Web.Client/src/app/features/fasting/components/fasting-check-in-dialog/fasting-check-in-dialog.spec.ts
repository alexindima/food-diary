import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { FastingFacade } from '../../lib/fasting.facade';
import { FastingCheckInDialogComponent } from './fasting-check-in-dialog';

const DEFAULT_CHECK_IN_LEVEL = 3;

describe('FastingCheckInDialogComponent', () => {
    let fixture: ComponentFixture<FastingCheckInDialogComponent>;
    let dialogRef: { close: ReturnType<typeof vi.fn> };
    let savedVersion: ReturnType<typeof signal<number>>;
    let saveCheckIn: ReturnType<typeof vi.fn>;

    beforeEach(async () => {
        dialogRef = { close: vi.fn() };
        savedVersion = signal(0);
        saveCheckIn = vi.fn();

        await TestBed.configureTestingModule({
            imports: [FastingCheckInDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: dialogRef },
                {
                    provide: FastingFacade,
                    useValue: {
                        checkInSavedVersion: savedVersion,
                        isSavingCheckIn: signal(false),
                        isEnding: signal(false),
                        isUpdatingCycle: signal(false),
                        hungerLevel: signal(DEFAULT_CHECK_IN_LEVEL),
                        energyLevel: signal(DEFAULT_CHECK_IN_LEVEL),
                        moodLevel: signal(DEFAULT_CHECK_IN_LEVEL),
                        selectedSymptoms: signal<string[]>([]),
                        checkInNotes: signal(''),
                        saveCheckIn,
                    },
                },
            ],
        })
            .overrideComponent(FastingCheckInDialogComponent, { set: { template: '' } })
            .compileComponents();

        fixture = TestBed.createComponent(FastingCheckInDialogComponent);
        fixture.detectChanges();
    });

    it('delegates saving and closes after the save completes', () => {
        fixture.componentInstance['save']();
        expect(saveCheckIn).toHaveBeenCalledTimes(1);

        savedVersion.set(1);
        fixture.detectChanges();

        expect(dialogRef.close).toHaveBeenCalledWith('saved');
    });
});
