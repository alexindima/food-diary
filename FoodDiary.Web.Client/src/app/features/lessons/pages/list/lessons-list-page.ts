import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective, FdUiInputComponent, FdUiLevelIndicatorComponent, FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { merge, startWith } from 'rxjs';

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
        FdUiIconComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        LessonsListFiltersComponent,
        LessonsListGridComponent,
        LessonsListProgressComponent,
        FdUiLevelIndicatorComponent,
        FdUiPaginationComponent,
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
    private readonly translateService = inject(TranslateService);
    private readonly translationChange = toSignal(
        merge(this.translateService.onLangChange, this.translateService.onTranslationChange).pipe(startWith(null)),
        { initialValue: null },
    );
    protected readonly facade = inject(LessonFacade);
    protected readonly progress = computed(() => {
        const page = this.facade.page();
        return buildLessonProgress(page.readLessonCount, page.totalLessonCount);
    });
    protected readonly lessons = computed(() => buildLessonListItems(this.facade.lessons()));
    protected readonly selectedCategory = this.facade.categoryFilter;
    protected readonly searchQuery = this.facade.searchQuery;
    protected readonly difficultyFilter = this.facade.difficultyFilter;
    protected readonly sortOrder = this.facade.sortOrder;
    protected readonly difficultyOptions = computed<Array<FdUiSelectOption<string | null>>>(() => {
        this.translationChange();
        return [
            { value: null, label: this.translateService.instant('LESSONS.ALL_LEVELS') },
            { value: 'Beginner', label: this.translateService.instant('LESSONS.DIFFICULTY.Beginner') },
            { value: 'Intermediate', label: this.translateService.instant('LESSONS.DIFFICULTY.Intermediate') },
            { value: 'Advanced', label: this.translateService.instant('LESSONS.DIFFICULTY.Advanced') },
        ];
    });
    protected readonly sortOptions = computed<Array<FdUiSelectOption<'recommended' | 'shortest'>>>(() => {
        this.translationChange();
        return [
            { value: 'recommended', label: this.translateService.instant('LESSONS.SORT_RECOMMENDED') },
            { value: 'shortest', label: this.translateService.instant('LESSONS.SORT_SHORTEST') },
        ];
    });
    protected readonly nextLesson = computed(() => this.lessons().find(lesson => !lesson.isRead) ?? this.lessons().at(0) ?? null);
    protected readonly recommendedLesson = computed(() => {
        const nextLesson = this.nextLesson();
        return this.lessons().find(lesson => !lesson.isRead && lesson.id !== nextLesson?.id) ?? nextLesson;
    });

    public constructor() {
        this.facade.loadLessons();
    }

    protected filterByCategory(category: string | null): void {
        this.selectedCategory.set(category);
        this.facade.resetPage();
    }

    protected updateSearch(value: string | number | null): void {
        this.searchQuery.set(typeof value === 'string' ? value : '');
        this.facade.resetPage();
    }

    protected updateDifficulty(value: string | null): void {
        this.difficultyFilter.set(value);
        this.facade.resetPage();
    }

    protected updateSortOrder(value: 'recommended' | 'shortest' | null): void {
        this.sortOrder.set(value === 'shortest' ? 'shortest' : 'recommended');
        this.facade.resetPage();
    }

    protected openLesson(id: string): void {
        void this.router.navigate(['/lessons', id]);
    }

    protected startLessonsListTour(force = true): void {
        this.tourService.start(this.localizedTour.build(LESSONS_LIST_TOUR), { force });
    }
}
