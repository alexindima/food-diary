import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { ClientSummary } from '../../../../../shared/models/dietologist.data';
import type { ClientCardViewModel } from '../dietologist-clients-lib/dietologist-clients.types';

@Component({
    selector: 'fd-dietologist-client-card',
    imports: [NgOptimizedImage, TranslatePipe, FdUiCardComponent],
    templateUrl: './dietologist-client-card.html',
    styleUrl: '../dietologist-clients-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistClientCardComponent {
    public readonly item = input.required<ClientCardViewModel>();

    public readonly clientOpen = output<ClientSummary>();

    protected openClient(): void {
        this.clientOpen.emit(this.item().client);
    }

    protected genderTranslationKey(value: string): string {
        const normalized = value.toUpperCase();
        const key = normalized === 'MALE' ? 'M' : normalized === 'FEMALE' ? 'F' : normalized;
        return `USER_MANAGE.GENDER_OPTIONS.${key}`;
    }

    protected activityTranslationKey(value: string): string {
        return `USER_MANAGE.ACTIVITY_LEVEL_OPTIONS.${value.toUpperCase()}`;
    }
}
