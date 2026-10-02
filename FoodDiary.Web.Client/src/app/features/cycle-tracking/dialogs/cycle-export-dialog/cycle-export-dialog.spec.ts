import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { CycleExportDialogComponent, type CycleExportDialogData } from './cycle-export-dialog';

function setup(data: Partial<CycleExportDialogData> = {}): {
    fixture: ComponentFixture<CycleExportDialogComponent>;
    close: ReturnType<typeof vi.fn>;
    component: CycleExportDialogComponent;
} {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [CycleExportDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FdUiDialogRef, useValue: { close } },
            {
                provide: FD_UI_DIALOG_DATA,
                useValue: { sensitive: false, today: '2026-10-02', dateFrom: '2025-10-01', dateTo: '2026-10-02', ...data },
            },
        ],
    });
    const fixture = TestBed.createComponent(CycleExportDialogComponent);
    fixture.detectChanges();
    return { fixture, close, component: fixture.componentInstance };
}

describe('CycleExportDialogComponent', () => {
    it('returns the chosen historical range without requesting a password for standard export', () => {
        const { fixture, close } = setup();
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[type="submit"]')?.click();
        expect(close).toHaveBeenCalledExactlyOnceWith({ dateFrom: '2025-10-01', dateTo: '2026-10-02' });
        expect((fixture.nativeElement as HTMLElement).querySelector('input[type="password"]')).toBeNull();
    });

    it('keeps an overlong history open so the user can choose a smaller range', () => {
        const { fixture, close } = setup({ dateFrom: '2025-09-30' });
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[type="submit"]')?.click();
        fixture.detectChanges();
        expect(close).not.toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('CYCLE_TRACKING.EXPORT_RANGE_TOO_LONG');
    });

    it('requires a password before returning a sensitive range', () => {
        const { fixture, close, component } = setup({ sensitive: true });
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[type="submit"]')?.click();
        expect(close).not.toHaveBeenCalled();
        component['model'].update(value => ({ ...value, currentPassword: 'test-password' }));
        fixture.detectChanges();
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[type="submit"]')?.click();
        expect(close).toHaveBeenCalledExactlyOnceWith({ dateFrom: '2025-10-01', dateTo: '2026-10-02', currentPassword: 'test-password' });
    });

    it('returns no request when cancelled', () => {
        const { component, close } = setup({ sensitive: true });
        component['cancel']();
        expect(close).toHaveBeenCalledExactlyOnceWith(null);
    });
});
