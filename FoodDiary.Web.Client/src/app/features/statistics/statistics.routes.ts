import { Routes } from '@angular/router';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { StatisticsComponent } from './pages/statistics.component';

const routes: Routes = [
    {
        path: '',
        component: StatisticsComponent,
        providers: [provideCharts(withDefaultRegisterables())],
    },
];

export default routes;
