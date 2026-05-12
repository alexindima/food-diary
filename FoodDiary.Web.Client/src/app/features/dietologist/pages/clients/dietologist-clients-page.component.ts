import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import type { ClientSummary } from '../../../../shared/models/dietologist.data';
import { DietologistService } from '../../api/dietologist.service';
import type { ClientCardViewModel } from './dietologist-clients.types';
import { DietologistClientsListComponent } from './dietologist-clients-list.component';

@Component({
    selector: 'fd-dietologist-clients-page',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [DietologistClientsListComponent],
    templateUrl: './dietologist-clients-page.component.html',
    styleUrls: ['./dietologist-clients-page.component.scss'],
})
export class DietologistClientsPageComponent {
    private readonly dietologistService = inject(DietologistService);
    private readonly router = inject(Router);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly languageVersion = signal(0);

    public readonly clients = signal<ClientSummary[]>([]);
    public readonly loading = signal(true);
    public readonly clientItems = computed<ClientCardViewModel[]>(() => {
        this.languageVersion();

        return this.clients().map(client => ({
            client,
            title: this.getClientTitle(client),
            initials: this.getClientInitials(client),
            connectedDateLabel: this.formatMediumDate(client.acceptedAtUtc),
        }));
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });

        this.dietologistService.getMyClients().subscribe({
            next: clients => {
                this.clients.set(clients);
                this.loading.set(false);
            },
            error: () => {
                this.loading.set(false);
            },
        });
    }

    public openClient(client: ClientSummary): void {
        void this.router.navigate(['/dietologist', 'clients', client.userId]);
    }

    private getClientTitle(client: ClientSummary): string {
        const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
        return fullName.length > 0 ? fullName : client.email;
    }

    private getClientInitials(client: ClientSummary): string {
        const parts = [client.firstName, client.lastName].filter((value): value is string => Boolean(value?.trim()));
        if (parts.length === 0) {
            return client.email.charAt(0).toUpperCase();
        }

        return parts
            .slice(0, 2)
            .map(value => value.trim().charAt(0).toUpperCase())
            .join('');
    }

    private formatMediumDate(value: string): string {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
        }

        return new Intl.DateTimeFormat(resolveAppLocale(this.translateService.getCurrentLang()), {
            dateStyle: 'medium',
        }).format(date);
    }
}
