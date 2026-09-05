import type { DestroyRef, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import type { ExportService } from '../../../shared/api/export.service';
import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import type { CycleResponse } from '../models/cycle.data';
import { toCycleDateKey } from './cycle-tracking.mapper';

export function runCycleExport(
    state: { cycle: CycleResponse | null; exporting: WritableSignal<boolean> },
    service: Pick<ExportService, 'exportCycle' | 'exportSensitiveCycle'>,
    destroyRef: DestroyRef,
    currentPassword?: string,
): void {
    const { cycle, exporting } = state;
    if (currentPassword === '' || cycle === null || exporting()) {
        return;
    }

    const range = {
        dateFrom: toCycleDateKey(cycle.trackingStartDate),
        dateTo: formatDateInputValue(new Date()),
        timeZoneOffsetMinutes: -new Date().getTimezoneOffset(),
    };
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
        .subscribe();
}
