import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FastingFacade } from '../../lib/fasting.facade';
import { FastingSessionManagementDialogComponent } from './fasting-session-management-dialog';

describe('FastingSessionManagementDialogComponent lifecycle', () => {
    const active = signal(true);
    const close = vi.fn();
    let fixture: ComponentFixture<FastingSessionManagementDialogComponent>;

    beforeEach(async () => {
        active.set(true);
        close.mockClear();
        await TestBed.configureTestingModule({
            imports: [FastingSessionManagementDialogComponent],
            providers: [
                { provide: FastingFacade, useValue: { isActive: active } },
                { provide: FdUiDialogRef, useValue: { close } },
            ],
        })
            .overrideComponent(FastingSessionManagementDialogComponent, { set: { template: '' } })
            .compileComponents();
        fixture = TestBed.createComponent(FastingSessionManagementDialogComponent);
        fixture.detectChanges();
    });

    it('keeps management available while the session remains active', () => {
        fixture.detectChanges();
        expect(close).not.toHaveBeenCalled();
    });

    it('closes after the active session ends', () => {
        active.set(false);
        fixture.detectChanges();
        expect(close).toHaveBeenCalledTimes(1);
    });
});
