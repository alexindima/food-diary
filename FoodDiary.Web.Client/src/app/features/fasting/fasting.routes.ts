import type { Routes } from '@angular/router';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { FastingPageComponent } from './pages/fasting-page/fasting-page.component';

const routes: Routes = [
    {
        path: '',
        component: FastingPageComponent,
        providers: [provideCharts(withDefaultRegisterables())],
    },
];

export default routes;
