import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';
import type { LessonListItem } from './lessons-list.types';

@Component({
    selector: 'fd-lessons-list-grid',
    imports: [TranslatePipe, FdUiIconComponent, FdUiLoaderComponent, FdCardHoverDirective],
    templateUrl: './lessons-list-grid.component.html',
    styleUrl: './lessons-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListGridComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly lessons = input.required<LessonListItem[]>();

    public readonly lessonOpen = output<string>();
}
