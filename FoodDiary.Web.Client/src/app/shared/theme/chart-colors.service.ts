import { DOCUMENT } from '@angular/common';
import { inject, Service } from '@angular/core';

import { type ChartColorPalette, createChartColorPalette } from '../../constants/chart-colors';

@Service()
export class ChartColorsService {
    private readonly document = inject(DOCUMENT);

    public get palette(): ChartColorPalette {
        return createChartColorPalette(this.document);
    }
}
