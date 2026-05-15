import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { LessonFacade } from '../../lib/lesson.facade';
import { buildLessonListItems, buildLessonProgress } from '../../lib/lesson-view.mapper';
import { LessonsListFiltersComponent } from './lessons-list-sections/lessons-list-filters/lessons-list-filters.component';
import { LessonsListGridComponent } from './lessons-list-sections/lessons-list-grid/lessons-list-grid.component';
import { LessonsListProgressComponent } from './lessons-list-sections/lessons-list-progress/lessons-list-progress.component';

@Component({
    selector: 'fd-lessons-list-page',
    imports: [
        TranslatePipe,
        PageBodyComponent,
        FdPageContainerDirective,
        LessonsListFiltersComponent,
        LessonsListGridComponent,
        LessonsListProgressComponent,
    ],
    providers: [LessonFacade],
    templateUrl: './lessons-list-page.component.html',
    styleUrl: './lessons-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListPageComponent {
    private readonly router = inject(Router);
    public readonly facade = inject(LessonFacade);
    public readonly progress = computed(() => buildLessonProgress(this.facade.lessons()));
    public readonly lessons = computed(() => buildLessonListItems(this.facade.lessons()));

    public constructor() {
        this.facade.loadLessons();
    }

    public filterByCategory(category: string | null): void {
        this.facade.loadLessons(category);
    }

    public openLesson(id: string): void {
        void this.router.navigate(['/lessons', id]);
    }
}
