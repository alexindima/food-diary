import { Routes } from '@angular/router';
import { StatisticsComponent } from './pages/statistics.component';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

const routes: Routes = [
    {
        path: '',
        component: StatisticsComponent,
        providers: [provideCharts(withDefaultRegisterables())],
    },
];

export default routes;
