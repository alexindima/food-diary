import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { fdUiCoerceInputTextValue, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import type { AdminRecipeModerationAction } from '../lib/admin-recipe-moderation.facade';
import type { AdminRecipeModerationItem } from '../models/admin-recipe-moderation.data';
import { RECIPE_MODERATION_STATUS_LABELS } from '../models/admin-recipe-moderation.data';
import { AdminRecipeModerationEvidenceComponent } from './admin-recipe-moderation-evidence';

@Component({
    selector: 'fd-admin-recipe-moderation-detail',
    imports: [
        AdminLoadErrorComponent,
        AdminRecipeModerationEvidenceComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        FdUiTextareaComponent,
        TranslatePipe,
    ],
    templateUrl: './admin-recipe-moderation-detail.html',
    styleUrl: './admin-recipe-moderation-detail.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminRecipeModerationDetailComponent {
    public readonly item = input<AdminRecipeModerationItem | null>(null);
    public readonly loading = input(false);
    public readonly failed = input(false);
    public readonly actionInProgress = input(false);
    public readonly actionError = input<string | null>(null);
    public readonly reason = model('');
    public readonly retryDetails = output();
    public readonly submitAction = output<AdminRecipeModerationAction>();
    protected readonly statusLabels = RECIPE_MODERATION_STATUS_LABELS;

    protected updateReason(value: FdUiInputValue): void {
        this.reason.set(fdUiCoerceInputTextValue(value));
    }
}
