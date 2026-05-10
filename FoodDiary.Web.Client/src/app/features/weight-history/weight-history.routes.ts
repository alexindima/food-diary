import type { Routes } from '@angular/router';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { WeightHistoryPageComponent } from './pages/weight-history-page.component';

const routes: Routes = [
    {
        path: '',
        component: WeightHistoryPageComponent,
        providers: [provideCharts(withDefaultRegisterables())],
    },
];

export default routes;
