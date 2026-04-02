import { Routes } from '@angular/router';
import { WeightHistoryPageComponent } from './pages/weight-history-page.component';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

const routes: Routes = [
    {
        path: '',
        component: WeightHistoryPageComponent,
        providers: [provideCharts(withDefaultRegisterables())],
    },
];

export default routes;
