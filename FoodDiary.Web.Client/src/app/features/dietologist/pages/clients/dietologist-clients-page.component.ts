import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DietologistService } from '../../api/dietologist.service';
import { ClientSummary } from '../../models/dietologist.data';
import { DatePipe } from '@angular/common';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-dietologist-clients-page',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [DatePipe, FdUiCardComponent],
    template: `
        <div class="dietologist-clients">
            <h1 class="dietologist-clients__title">My Clients</h1>

            @if (loading()) {
                <p>Loading...</p>
            } @else if (clients().length === 0) {
                <p class="dietologist-clients__empty">No clients yet. Clients can invite you by email.</p>
            } @else {
                <div class="dietologist-clients__list">
                    @for (client of clients(); track client.userId) {
                        <fd-ui-card class="dietologist-clients__card" (click)="openClient(client)">
                            <div class="dietologist-clients__card-content">
                                <div class="dietologist-clients__info">
                                    <span class="dietologist-clients__name">
                                        {{ client.firstName || '' }} {{ client.lastName || '' }}
                                    </span>
                                    <span class="dietologist-clients__email">{{ client.email }}</span>
                                </div>
                                <span class="dietologist-clients__date"> Connected {{ client.acceptedAtUtc | date: 'mediumDate' }} </span>
                            </div>
                        </fd-ui-card>
                    }
                </div>
            }
        </div>
    `,
    styles: `
        .dietologist-clients {
            padding: 24px;
            max-width: 800px;
            margin: 0 auto;

            &__title {
                font-size: 24px;
                font-weight: 600;
                margin: 0 0 24px;
            }

            &__empty {
                color: var(--fd-text-secondary);
            }

            &__list {
                display: flex;
                flex-direction: column;
                gap: 12px;
            }

            &__card {
                cursor: pointer;
            }

            &__card-content {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 16px;
            }

            &__info {
                display: flex;
                flex-direction: column;
                gap: 4px;
            }

            &__name {
                font-weight: 600;
                font-size: 16px;
            }

            &__email {
                color: var(--fd-text-secondary);
                font-size: 14px;
            }

            &__date {
                color: var(--fd-text-secondary);
                font-size: 13px;
            }
        }
    `,
})
export class DietologistClientsPageComponent implements OnInit {
    private readonly dietologistService = inject(DietologistService);
    private readonly router = inject(Router);

    public readonly clients = signal<ClientSummary[]>([]);
    public readonly loading = signal(true);

    public ngOnInit(): void {
        this.dietologistService.getMyClients().subscribe({
            next: clients => {
                this.clients.set(clients);
                this.loading.set(false);
            },
            error: () => this.loading.set(false),
        });
    }

    public openClient(client: ClientSummary): void {
        void this.router.navigate(['/dietologist', 'clients', client.userId]);
    }
}
