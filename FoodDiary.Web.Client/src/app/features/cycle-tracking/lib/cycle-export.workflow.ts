import type { DestroyRef, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import type { ExportService } from '../../../shared/api/export.service';
import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import type { CycleResponse } from '../models/cycle.data';
import type { CycleExportRange } from './cycle-export-range';
import { toCycleDateKey } from './cycle-tracking.mapper';

export function runCycleExport(
    state: { cycle: CycleResponse | null; exporting: WritableSignal<boolean>; error: WritableSignal<string | null> },
    service: Pick<ExportService, 'exportCycle' | 'exportSensitiveCycle'>,
    destroyRef: DestroyRef,
    options: { currentPassword?: string; range?: CycleExportRange } = {},
): void {
    const { cycle, exporting } = state;
    const { currentPassword, range: selectedRange } = options;
    if (currentPassword === '' || cycle === null || exporting()) {
        return;
    }

    const range = {
        dateFrom: selectedRange?.dateFrom ?? toCycleDateKey(cycle.trackingStartDate),
        dateTo: selectedRange?.dateTo ?? formatDateInputValue(new Date()),
        timeZoneOffsetMinutes: -new Date().getTimezoneOffset(),
    };
    state.error.set(null);
    exporting.set(true);
    const request =
        currentPassword === undefined ? service.exportCycle(range) : service.exportSensitiveCycle({ ...range, currentPassword });
    request
        .pipe(
            finalize(() => {
                exporting.set(false);
            }),
            takeUntilDestroyed(destroyRef),
        )
        .subscribe({
            error: () => {
                state.error.set(currentPassword === undefined ? 'CYCLE_TRACKING.EXPORT_FAILED' : 'CYCLE_TRACKING.SENSITIVE_EXPORT_FAILED');
            },
        });
}
