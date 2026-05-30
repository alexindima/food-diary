import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { FdCardHoverDirective } from '../../../../../../shared/ui/card-hover.directive';
import type { LessonListItemViewModel } from '../../../../lib/lesson-view.mapper';

@Component({
    selector: 'fd-lessons-list-grid',
    imports: [TranslatePipe, FdUiIconComponent, FdUiLoaderComponent, FdCardHoverDirective],
    templateUrl: './lessons-list-grid.html',
    styleUrl: '../../lessons-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListGridComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly lessons = input.required<LessonListItemViewModel[]>();

    public readonly lessonOpen = output<string>();
}
