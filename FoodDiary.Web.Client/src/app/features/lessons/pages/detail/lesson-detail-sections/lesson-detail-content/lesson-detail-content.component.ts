import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import type { LessonDetailViewModel } from '../../../../lib/lesson-view.mapper';

@Component({
    selector: 'fd-lesson-detail-content',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, FdUiLoaderComponent],
    templateUrl: './lesson-detail-content.component.html',
    styleUrl: '../../lesson-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonDetailContentComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly lesson = input.required<LessonDetailViewModel | null>();
    public readonly markRead = output();
}
