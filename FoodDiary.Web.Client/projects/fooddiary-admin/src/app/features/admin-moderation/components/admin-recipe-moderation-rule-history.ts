import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { AdminRecipeModerationPolicyVersion, RecipeModerationPolicyId } from '../models/admin-recipe-moderation.data';

@Component({
    selector: 'fd-admin-recipe-moderation-rule-history',
    imports: [DatePipe, FdUiButtonComponent, TranslatePipe],
    templateUrl: './admin-recipe-moderation-rule-history.html',
    styleUrl: './admin-recipe-moderation-rule-history.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminRecipeModerationRuleHistoryComponent {
    public readonly history = input.required<readonly AdminRecipeModerationPolicyVersion[]>();
    public readonly activeId = input.required<RecipeModerationPolicyId>();
    public readonly categoryLabels = input.required<Readonly<Record<string, string>>>();
    public readonly saving = input(false);
    public readonly restore = output<RecipeModerationPolicyId>();
}
