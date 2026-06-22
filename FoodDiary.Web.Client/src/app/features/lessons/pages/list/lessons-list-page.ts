import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { LessonFacade } from '../../lib/lesson.facade';
import { buildLessonListItems, buildLessonProgress } from '../../lib/lesson-view.mapper';
import { LessonsListFiltersComponent } from './lessons-list-sections/lessons-list-filters/lessons-list-filters';
import { LessonsListGridComponent } from './lessons-list-sections/lessons-list-grid/lessons-list-grid';
import { LessonsListProgressComponent } from './lessons-list-sections/lessons-list-progress/lessons-list-progress';
import { LESSONS_LIST_TOUR } from './lessons-list-tour';

@Component({
    selector: 'fd-lessons-list-page',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        LessonsListFiltersComponent,
        LessonsListGridComponent,
        LessonsListProgressComponent,
    ],
    providers: [LessonFacade],
    templateUrl: './lessons-list-page.html',
    styleUrl: './lessons-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListPageComponent {
    private readonly router = inject(Router);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    protected readonly facade = inject(LessonFacade);
    protected readonly progress = computed(() => buildLessonProgress(this.facade.lessons()));
    protected readonly lessons = computed(() => buildLessonListItems(this.facade.lessons()));

    public constructor() {
        this.facade.loadLessons();
    }

    protected filterByCategory(category: string | null): void {
        this.facade.loadLessons(category);
    }

    protected openLesson(id: string): void {
        void this.router.navigate(['/lessons', id]);
    }

    protected startLessonsListTour(force = true): void {
        this.tourService.start(this.localizedTour.build(LESSONS_LIST_TOUR), { force });
    }
}
