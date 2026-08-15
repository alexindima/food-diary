import { inject, Pipe, type PipeTransform } from '@angular/core';

import { type MeasurementSystem, MeasurementSystemService } from './measurement-system.service';

export type MeasurementKind = 'weight' | 'length';

// The selected unit system is signal-backed state rather than a pipe input on legacy templates.
// eslint-disable-next-line @angular-eslint/no-pipe-impure -- The persisted preference can change without a numeric input change.
@Pipe({ name: 'measurementValue', pure: false })
export class MeasurementValuePipe implements PipeTransform {
    private readonly measurements = inject(MeasurementSystemService);

    public transform(value: number | null | undefined, kind: MeasurementKind, system?: MeasurementSystem): number | null {
        if (value === null || value === undefined) {
            return null;
        }

        const resolvedSystem = system ?? this.measurements.system();
        if (resolvedSystem === 'metric') {
            return value;
        }

        return kind === 'weight' ? this.measurements.displayWeight(value) : this.measurements.displayLength(value);
    }
}

// See MeasurementValuePipe: this keeps existing views reactive when the preference changes.
// eslint-disable-next-line @angular-eslint/no-pipe-impure -- Unit labels must update immediately with the persisted preference.
@Pipe({ name: 'measurementUnit', pure: false })
export class MeasurementUnitPipe implements PipeTransform {
    private readonly measurements = inject(MeasurementSystemService);

    public transform(kind: MeasurementKind, system?: MeasurementSystem): string {
        const resolvedSystem = system ?? this.measurements.system();
        if (kind === 'weight') {
            return resolvedSystem === 'imperial' ? 'GENERAL.UNITS.LB' : 'GENERAL.UNITS.KG';
        }

        return resolvedSystem === 'imperial' ? 'GENERAL.UNITS.IN' : 'GENERAL.UNITS.CM';
    }
}
