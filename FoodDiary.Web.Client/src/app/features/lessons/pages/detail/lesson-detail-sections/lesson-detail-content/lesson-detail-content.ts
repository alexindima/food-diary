import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiLevelIndicatorComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiEmptyStateComponent } from 'fd-ui-kit/empty-state/fd-ui-empty-state';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import type { LessonDetailViewModel } from '../../../../lib/lesson-view.mapper';

@Component({
    selector: 'fd-lesson-detail-content',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiEmptyStateComponent,
        FdUiIconComponent,
        FdUiLevelIndicatorComponent,
        FdUiLoaderComponent,
    ],
    templateUrl: './lesson-detail-content.html',
    styleUrl: './lesson-detail-content.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonDetailContentComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly lesson = input.required<LessonDetailViewModel | null>();
    public readonly markRead = output();
}
