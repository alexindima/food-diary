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
                                <div class="dietologist-clients__avatar" [class.dietologist-clients__avatar--image]="!!client.profileImage">
                                    @if (client.profileImage) {
                                        <img [src]="client.profileImage" [alt]="getClientTitle(client)" />
                                    } @else {
                                        <span>{{ getClientInitials(client) }}</span>
                                    }
                                </div>
                                <div class="dietologist-clients__info">
                                    <span class="dietologist-clients__name">{{ getClientTitle(client) }}</span>
                                    <span class="dietologist-clients__email">{{ client.email }}</span>
                                    @if (client.permissions.shareProfile) {
                                        <div class="dietologist-clients__meta">
                                            @if (client.height) {
                                                <span class="dietologist-clients__chip">{{ client.height }} cm</span>
                                            }
                                            @if (client.gender) {
                                                <span class="dietologist-clients__chip">{{ client.gender }}</span>
                                            }
                                            @if (client.activityLevel) {
                                                <span class="dietologist-clients__chip">{{ client.activityLevel }}</span>
                                            }
                                        </div>
                                    }
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
                gap: 16px;
            }

            &__info {
                display: flex;
                flex-direction: column;
                gap: 4px;
                min-width: 0;
                flex: 1;
            }

            &__avatar {
                width: 52px;
                height: 52px;
                border-radius: 50%;
                background: linear-gradient(145deg, #dbeafe, #eff6ff);
                color: #1d4ed8;
                display: inline-flex;
                align-items: center;
                justify-content: center;
                font-size: 18px;
                font-weight: 700;
                overflow: hidden;
                flex-shrink: 0;

                img {
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                }
            }

            &__meta {
                display: flex;
                flex-wrap: wrap;
                gap: 8px;
            }

            &__chip {
                display: inline-flex;
                align-items: center;
                padding: 4px 10px;
                border-radius: 999px;
                background: #eff6ff;
                color: #1d4ed8;
                font-size: 12px;
                font-weight: 600;
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

    public getClientTitle(client: ClientSummary): string {
        const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
        return fullName || client.email;
    }

    public getClientInitials(client: ClientSummary): string {
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
