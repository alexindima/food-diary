import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { AiInputActionBarComponent } from '../../../../../components/shared/ai-input-bar/ai-input-action-bar';
import type { AiInputBarResult } from '../../../../../components/shared/ai-input-bar/ai-input-bar.types';

@Component({
    selector: 'fd-dashboard-quick-add',
    imports: [TranslatePipe, AiInputActionBarComponent],
    templateUrl: './dashboard-quick-add.html',
    styleUrl: '../../dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardQuickAddComponent {
    public readonly isAiMealSaving = input(false);
    public readonly aiMealClearToken = input(0);
    public readonly mealCreateRequested = output<AiInputBarResult>();
    public readonly manualAdd = output();
}
