import { Routes } from '@angular/router';
import { WaistHistoryPageComponent } from './pages/waist-history-page.component';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

const routes: Routes = [
    {
        path: '',
        component: WaistHistoryPageComponent,
        providers: [provideCharts(withDefaultRegisterables())],
    },
];

export default routes;
