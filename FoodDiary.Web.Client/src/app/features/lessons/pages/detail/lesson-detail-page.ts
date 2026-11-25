import { ChangeDetectionStrategy, Component, computed, effect, inject, input, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { LessonFacade } from '../../lib/lesson.facade';
import { buildLessonDetailView } from '../../lib/lesson-view.mapper';
import { LessonDetailContentComponent } from './lesson-detail-sections/lesson-detail-content/lesson-detail-content';
import { LESSON_DETAIL_TOUR } from './lesson-detail-tour';

@Component({
    selector: 'fd-lesson-detail-page',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        LessonDetailContentComponent,
    ],
    providers: [LessonFacade],
    templateUrl: './lesson-detail-page.html',
    styleUrl: './lesson-detail-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonDetailPageComponent {
    public readonly id = input<string>();

    private readonly router = inject(Router);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    protected readonly facade = inject(LessonFacade);
    protected readonly lesson = computed(() => buildLessonDetailView(this.facade.selectedLesson()));

    public constructor() {
        effect(() => {
            const id = this.id();
            untracked(() => {
                this.facade.loadLesson(id === undefined || id.trim().length === 0 ? null : entityId<'lesson'>(id));
            });
        });
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

    protected startLessonDetailTour(force = true): void {
        this.tourService.start(this.localizedTour.build(LESSON_DETAIL_TOUR), { force });
    }
}
