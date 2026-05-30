import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { LessonFacade } from '../../lib/lesson.facade';
import { buildLessonDetailView } from '../../lib/lesson-view.mapper';
import { LessonDetailContentComponent } from './lesson-detail-sections/lesson-detail-content/lesson-detail-content';

@Component({
    selector: 'fd-lesson-detail-page',
    imports: [TranslatePipe, FdUiButtonComponent, PageBodyComponent, FdPageContainerDirective, LessonDetailContentComponent],
    providers: [LessonFacade],
    templateUrl: './lesson-detail-page.html',
    styleUrl: './lesson-detail-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonDetailPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    protected readonly facade = inject(LessonFacade);
    protected readonly lesson = computed(() => buildLessonDetailView(this.facade.selectedLesson()));

    public constructor() {
        const id = this.route.snapshot.paramMap.get('id');
        if (id !== null && id.trim().length > 0) {
            this.facade.loadLesson(id);
        }
    }

    protected markRead(): void {
        const lesson = this.facade.selectedLesson();
        if (lesson !== null && !lesson.isRead) {
            this.facade.markRead(lesson.id);
        }
    }

    protected goBack(): void {
        void this.router.navigate(['/lessons']);
    }
}
