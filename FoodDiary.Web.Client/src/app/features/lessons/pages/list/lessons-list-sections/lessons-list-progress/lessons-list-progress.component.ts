import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { LessonProgressViewModel } from '../../../../lib/lesson-view.mapper';

@Component({
    selector: 'fd-lessons-list-progress',
    imports: [TranslatePipe],
    templateUrl: './lessons-list-progress.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListProgressComponent {
    public readonly progress = input.required<LessonProgressViewModel | null>();
}
