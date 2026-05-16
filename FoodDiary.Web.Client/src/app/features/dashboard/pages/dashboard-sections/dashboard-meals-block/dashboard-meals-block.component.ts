import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import type { AiInputBarResult } from '../../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { MealsPreviewComponent } from '../../../../../components/shared/meals-preview/meals-preview.component';
import type { DashboardBlockState, DashboardMealPreviewEntry, DashboardMealsPreviewState } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-meals-block',
    imports: [MealsPreviewComponent],
    templateUrl: './dashboard-meals-block.component.html',
    styleUrl: '../../dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardMealsBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly entries = input.required<DashboardMealPreviewEntry[]>();
    public readonly previewState = input.required<DashboardMealsPreviewState>();

    public readonly blockToggle = output();
    public readonly viewAll = output();
    public readonly add = output<string | null | undefined>();
    public readonly aiMealCreateRequested = output<AiInputBarResult>();
    public readonly open = output<{ id: string }>();
}
