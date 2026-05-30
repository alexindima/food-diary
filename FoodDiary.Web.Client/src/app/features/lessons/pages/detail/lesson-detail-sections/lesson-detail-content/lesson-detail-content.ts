import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import type { LessonDetailViewModel } from '../../../../lib/lesson-view.mapper';

@Component({
    selector: 'fd-lesson-detail-content',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, FdUiLoaderComponent],
    templateUrl: './lesson-detail-content.html',
    styleUrl: '../../lesson-detail-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonDetailContentComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly lesson = input.required<LessonDetailViewModel | null>();
    public readonly markRead = output();
}
