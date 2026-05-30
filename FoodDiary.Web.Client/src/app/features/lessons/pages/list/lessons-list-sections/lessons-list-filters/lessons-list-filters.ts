import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { buildLessonCategoryOptions } from '../../../../lib/lesson-view.mapper';

@Component({
    selector: 'fd-lessons-list-filters',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './lessons-list-filters.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonsListFiltersComponent {
    public readonly selectedCategory = input.required<string | null>();
    protected readonly options = computed(() => buildLessonCategoryOptions(this.selectedCategory()));
    public readonly filterChange = output<string | null>();
}
