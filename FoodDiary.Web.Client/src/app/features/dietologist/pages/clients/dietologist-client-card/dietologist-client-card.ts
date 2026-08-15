import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
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
    private readonly measurements = inject(MeasurementSystemService);

    public readonly item = input.required<ClientCardViewModel>();

    public readonly clientOpen = output<ClientSummary>();

    protected readonly displayHeight = computed(() => {
        const heightCm = this.item().client.heightCm;
        if (heightCm === null) {
            return null;
        }

        if (this.measurements.system() === 'metric') {
            return `${heightCm} cm`;
        }

        const height = this.measurements.displayHeight(heightCm);
        return `${height.feet} ft ${height.inches} in`;
    });

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
