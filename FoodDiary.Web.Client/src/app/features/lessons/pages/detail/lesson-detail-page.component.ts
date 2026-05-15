import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { LessonFacade } from '../../lib/lesson.facade';
import { buildLessonDetailView } from '../../lib/lesson-view.mapper';
import { LessonDetailContentComponent } from './lesson-detail-sections/lesson-detail-content/lesson-detail-content.component';

@Component({
    selector: 'fd-lesson-detail-page',
    imports: [TranslatePipe, FdUiButtonComponent, PageBodyComponent, FdPageContainerDirective, LessonDetailContentComponent],
    providers: [LessonFacade],
    templateUrl: './lesson-detail-page.component.html',
    styleUrl: './lesson-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonDetailPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly facade = inject(LessonFacade);
    public readonly lesson = computed(() => buildLessonDetailView(this.facade.selectedLesson()));

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
