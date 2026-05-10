import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { LessonFacade } from '../../lib/lesson.facade';
import type { LessonDetail } from '../../models/lesson.data';

interface LessonDetailState extends LessonDetail {
    categoryLabelKey: string;
    difficultyLabelKey: string;
}

@Component({
    selector: 'fd-lesson-detail-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiLoaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
    ],
    providers: [LessonFacade],
    templateUrl: './lesson-detail-page.component.html',
    styleUrl: './lesson-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonDetailPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly facade = inject(LessonFacade);
    public readonly lesson = computed<LessonDetailState | null>(() => {
        const lesson = this.facade.selectedLesson();
        if (lesson === null) {
            return null;
        }

        return {
            ...lesson,
            categoryLabelKey: `LESSONS.CATEGORY.${lesson.category}`,
            difficultyLabelKey: `LESSONS.DIFFICULTY.${lesson.difficulty}`,
        };
    });

    public constructor() {
        const id = this.route.snapshot.paramMap.get('id');
        if (id !== null && id.trim().length > 0) {
            this.facade.loadLesson(id);
        }
    }

    public markRead(): void {
        const lesson = this.facade.selectedLesson();
        if (lesson !== null && !lesson.isRead) {
            this.facade.markRead(lesson.id);
        }
    }

    public goBack(): void {
        void this.router.navigate(['/lessons']);
    }
}
