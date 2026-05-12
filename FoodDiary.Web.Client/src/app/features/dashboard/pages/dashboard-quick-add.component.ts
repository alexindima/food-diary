import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { AiInputBarComponent } from '../../../components/shared/ai-input-bar/ai-input-bar.component';
import type { AiInputBarResult } from '../../../components/shared/ai-input-bar/ai-input-bar.types';

@Component({
    selector: 'fd-dashboard-quick-add',
    imports: [TranslatePipe, FdUiButtonComponent, AiInputBarComponent],
    templateUrl: './dashboard-quick-add.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardQuickAddComponent {
    public readonly mealCreateRequested = output<AiInputBarResult>();
    public readonly manualAdd = output();
}
