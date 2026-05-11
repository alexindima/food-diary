import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { LessonFacade } from '../../lib/lesson.facade';
import { LESSON_CATEGORIES, type LessonSummary } from '../../models/lesson.data';

const PERCENTAGE_MULTIPLIER = 100;

interface LessonCategoryOption {
    value: string | null;
    labelKey: string;
    fill: 'solid' | 'outline';
}

interface LessonListItem extends LessonSummary {
    categoryLabelKey: string;
    difficultyLabelKey: string;
}

@Component({
    selector: 'fd-lessons-list-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiLoaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FdCardHoverDirective,
    ],
    providers: [LessonFacade],
    templateUrl: './lessons-list-page.component.html',
    styleUrl: './lessons-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListPageComponent {
    private readonly router = inject(Router);
    public readonly facade = inject(LessonFacade);
    private readonly categoryDefinitions = [
        { value: null, labelKey: 'LESSONS.FILTER_ALL' },
        ...LESSON_CATEGORIES.map(category => ({
            value: category,
            labelKey: `LESSONS.CATEGORY.${category}`,
        })),
    ];

    public constructor() {
        this.facade.loadLessons();
    }

    public readonly progress = computed(() => {
        const all = this.facade.lessons();
        if (all.length === 0) {
            return null;
        }
        const read = all.filter(l => l.isRead).length;
        return { read, total: all.length, percent: Math.round((read / all.length) * PERCENTAGE_MULTIPLIER) };
    });
    public readonly categoryFilterOptions = computed<LessonCategoryOption[]>(() => {
        const selectedCategory = this.facade.categoryFilter();

        return this.categoryDefinitions.map(category => ({
            ...category,
            fill: selectedCategory === category.value ? 'solid' : 'outline',
        }));
    });
    public readonly lessons = computed<LessonListItem[]>(() =>
        this.facade.lessons().map(lesson => ({
            ...lesson,
            categoryLabelKey: `LESSONS.CATEGORY.${lesson.category}`,
            difficultyLabelKey: `LESSONS.DIFFICULTY.${lesson.difficulty}`,
        })),
    );

    public filterByCategory(category: string | null): void {
        this.facade.loadLessons(category);
    }

    public openLesson(id: string): void {
        void this.router.navigate(['/lessons', id]);
    }
}
