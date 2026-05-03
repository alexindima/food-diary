import { type Routes } from '@angular/router';

const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./pages/list/lessons-list-page.component').then(m => m.LessonsListPageComponent),
    },
    {
        path: ':id',
        loadComponent: () => import('./pages/detail/lesson-detail-page.component').then(m => m.LessonDetailPageComponent),
    },
];

export default routes;
