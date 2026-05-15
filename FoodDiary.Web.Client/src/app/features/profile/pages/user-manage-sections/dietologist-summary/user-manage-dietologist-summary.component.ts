import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import type { DietologistRelationship } from '../../../../../shared/models/dietologist.data';
import type { DietologistFormData } from '../../user-manage/user-manage.types';

type DietologistSummaryAction = {
    fill: 'outline' | 'solid';
    variant: 'primary' | 'secondary';
    icon: string;
    labelKey: string;
    disabled: boolean;
    action: 'invite' | 'revoke';
};

@Component({
    selector: 'fd-user-manage-dietologist-summary',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './user-manage-dietologist-summary.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageDietologistSummaryComponent {
    public readonly dietologistForm = input.required<FormGroup<DietologistFormData>>();
    public readonly dietologistRelationship = input.required<DietologistRelationship | null>();
    public readonly dietologistInviteEmailError = input.required<string | null>();
    public readonly dietologistAcceptedDateLabel = input.required<string | null>();
    public readonly dietologistExpiresDateLabel = input.required<string | null>();
    public readonly isSavingDietologist = input.required<boolean>();
    public readonly hasDietologistRelationship = input.required<boolean>();
    public readonly isDietologistPending = input.required<boolean>();
    public readonly isDietologistConnected = input.required<boolean>();

    public readonly dietologistInvite = output();
    public readonly dietologistRevoke = output();

    protected readonly labelView = computed(() => {
        if (this.isDietologistConnected()) {
            return {
                key: 'USER_MANAGE.DIETOLOGIST_CONNECTED_INFO',
                params: { email: this.dietologistRelationship()?.email },
            };
        }

        if (this.isDietologistPending()) {
            return { key: 'USER_MANAGE.DIETOLOGIST_PENDING_TITLE', params: null };
        }

        return { key: 'USER_MANAGE.DIETOLOGIST_EMPTY_TITLE', params: null };
    });

    protected readonly hintView = computed(() => {
        if (this.isDietologistPending()) {
            return {
                key: 'USER_MANAGE.DIETOLOGIST_PENDING_HINT',
                params: { email: this.dietologistRelationship()?.email },
            };
        }

        return { key: 'USER_MANAGE.DIETOLOGIST_EMPTY_HINT', params: null };
    });

    protected readonly summaryAction = computed<DietologistSummaryAction>(() => {
        if (this.isDietologistConnected()) {
            return {
                fill: 'outline',
                variant: 'secondary',
                icon: 'link_off',
                labelKey: 'USER_MANAGE.DIETOLOGIST_DISCONNECT_ACTION',
                disabled: this.isSavingDietologist(),
                action: 'revoke',
            };
        }

        if (this.hasDietologistRelationship()) {
            return {
                fill: 'outline',
                variant: 'secondary',
                icon: 'link_off',
                labelKey: 'USER_MANAGE.DIETOLOGIST_CANCEL_INVITE',
                disabled: this.isSavingDietologist(),
                action: 'revoke',
            };
        }

        return {
            fill: 'solid',
            variant: 'primary',
            icon: 'person_add',
            labelKey: 'USER_MANAGE.DIETOLOGIST_INVITE_ACTION',
            disabled: this.dietologistForm().invalid || this.isSavingDietologist(),
            action: 'invite',
        };
    });

    protected readonly showPendingStatus = computed(() => this.hasDietologistRelationship() && !this.isDietologistConnected());
    protected readonly pendingExpiresDate = computed(() => {
        if (!this.isDietologistPending()) {
            return null;
        }

        return this.dietologistExpiresDateLabel();
    });

    protected handleSummaryAction(action: DietologistSummaryAction['action']): void {
        if (action === 'invite') {
            this.dietologistInvite.emit();
            return;
        }

        this.dietologistRevoke.emit();
    }
}
