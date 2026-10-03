import { DestroyRef, inject, Injectable } from '@angular/core';

import { ExportService } from '../../../shared/api/export.service';
import { runCycleExport } from './cycle-export.workflow';
import type { CycleExportRange } from './cycle-export-range';
import { CycleTrackingStateFacade } from './cycle-tracking-state.facade';

@Injectable()
export class CycleExportFacade {
    private readonly destroyRef = inject(DestroyRef);

    private readonly exportService = inject(ExportService);
    private readonly state = inject(CycleTrackingStateFacade);
    public exportCycle(range?: CycleExportRange): void {
        runCycleExport(
            { cycle: this.state.cycle(), exporting: this.state.isExportingCycle, error: this.state.exportError },
            this.exportService,
            this.destroyRef,
            { range },
        );
    }

    public exportSensitiveCycle(currentPassword: string, range?: CycleExportRange): void {
        runCycleExport(
            { cycle: this.state.cycle(), exporting: this.state.isExportingCycle, error: this.state.exportError },
            this.exportService,
            this.destroyRef,
            { currentPassword, range },
        );
    }
}
