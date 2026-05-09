import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { DietologistService } from '../../api/dietologist.service';
import { type ClientSummary } from '../../models/dietologist.data';

interface ClientCardViewModel {
    client: ClientSummary;
    title: string;
    initials: string;
}

@Component({
    selector: 'fd-dietologist-clients-page',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [DatePipe, FdUiCardComponent],
    templateUrl: './dietologist-clients-page.component.html',
    styleUrls: ['./dietologist-clients-page.component.scss'],
})
export class DietologistClientsPageComponent {
    private readonly dietologistService = inject(DietologistService);
    private readonly router = inject(Router);

    public readonly clients = signal<ClientSummary[]>([]);
    public readonly loading = signal(true);
    public readonly clientItems = computed<ClientCardViewModel[]>(() =>
        this.clients().map(client => ({
            client,
            title: this.getClientTitle(client),
            initials: this.getClientInitials(client),
        })),
    );

    public constructor() {
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
        return fullName || client.email;
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
}
