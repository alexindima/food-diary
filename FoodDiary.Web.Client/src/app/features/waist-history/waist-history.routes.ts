import type { Routes } from '@angular/router';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { WaistHistoryPageComponent } from './pages/waist-history-page/waist-history-page.component';

const routes: Routes = [
    {
        path: '',
        component: WaistHistoryPageComponent,
        providers: [provideCharts(withDefaultRegisterables())],
    },
];

export default routes;
