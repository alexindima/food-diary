import type { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/list/lessons-list-page').then(m => m.LessonsListPageComponent),
    },
    {
        path: ':id',
        loadComponent: async () => import('./pages/detail/lesson-detail-page').then(m => m.LessonDetailPageComponent),
    },
];

export default routes;
