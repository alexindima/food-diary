import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiLevelIndicatorComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import type { LessonListItemViewModel, LessonProgressViewModel } from '../../../../lib/lesson-view.mapper';

@Component({
    selector: 'fd-lessons-list-progress',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, FdUiLevelIndicatorComponent],
    templateUrl: './lessons-list-progress.html',
    styleUrl: '../../lessons-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListProgressComponent {
    public readonly progress = input.required<LessonProgressViewModel | null>();
    public readonly nextLesson = input.required<LessonListItemViewModel | null>();
    public readonly lessonOpen = output<string>();
    protected readonly headingKey = computed(() => (this.progress()?.read === 0 ? 'LESSONS.START_LEARNING' : 'LESSONS.CONTINUE_LEARNING'));
    protected readonly actionKey = computed(() => (this.progress()?.read === 0 ? 'LESSONS.START' : 'LESSONS.CONTINUE'));
}
