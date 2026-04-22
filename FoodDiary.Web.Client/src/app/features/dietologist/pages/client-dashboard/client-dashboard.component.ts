import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DietologistService } from '../../api/dietologist.service';
import { ClientSummary } from '../../models/dietologist.data';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-client-dashboard',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiButtonComponent, FdUiCardComponent],
    template: `
        <div class="client-dashboard">
            <div class="client-dashboard__header">
                <fd-ui-button variant="secondary" fill="outline" (click)="goBack()">Back to clients</fd-ui-button>
                @if (client()) {
                    <h1 class="client-dashboard__title fd-page-title">{{ getClientTitle(client()!) }}</h1>
                    <span class="client-dashboard__email fd-ui-caption">{{ client()!.email }}</span>
                    @if (client()!.permissions.shareProfile) {
                        <div class="client-dashboard__profile-meta">
                            @if (client()!.height) {
                                <span class="client-dashboard__chip fd-ui-overline">{{ client()!.height }} cm</span>
                            }
                            @if (client()!.gender) {
                                <span class="client-dashboard__chip fd-ui-overline">{{ client()!.gender }}</span>
                            }
                            @if (client()!.activityLevel) {
                                <span class="client-dashboard__chip fd-ui-overline">{{ client()!.activityLevel }}</span>
                            }
                        </div>
                    }
                }
            </div>

            @if (loading()) {
                <p class="fd-ui-body-sm">Loading client data...</p>
            } @else if (!client()) {
                <p class="fd-ui-body-sm">Client not found.</p>
            } @else {
                <div class="client-dashboard__sections">
                    @if (client()!.permissions.shareProfile) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2 class="fd-ui-card-title">Profile</h2>
                                <p class="client-dashboard__placeholder fd-ui-body-sm">Client profile data will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareStatistics) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2 class="fd-ui-card-title">Statistics</h2>
                                <p class="client-dashboard__placeholder fd-ui-body-sm">Dashboard data will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareMeals) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2 class="fd-ui-card-title">Meals</h2>
                                <p class="client-dashboard__placeholder fd-ui-body-sm">Meal data will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareWeight) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2 class="fd-ui-card-title">Weight</h2>
                                <p class="client-dashboard__placeholder fd-ui-body-sm">Weight history will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareWaist) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2 class="fd-ui-card-title">Waist</h2>
                                <p class="client-dashboard__placeholder fd-ui-body-sm">Waist history will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareGoals) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2 class="fd-ui-card-title">Goals</h2>
                                <p class="client-dashboard__placeholder fd-ui-body-sm">Client goals will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareHydration) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2 class="fd-ui-card-title">Hydration</h2>
                                <p class="client-dashboard__placeholder fd-ui-body-sm">Hydration data will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareFasting) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2 class="fd-ui-card-title">Fasting</h2>
                                <p class="client-dashboard__placeholder fd-ui-body-sm">Fasting data will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (!hasAnyPermission()) {
                        <p class="client-dashboard__no-data fd-ui-body-sm">Client has not shared any data.</p>
                    }
                </div>
            }
        </div>
    `,
    styles: `
        .client-dashboard {
            padding: 24px;
            max-width: 800px;
            margin: 0 auto;

            &__header {
                display: flex;
                flex-direction: column;
                gap: 8px;
                margin-bottom: 24px;
            }

            &__title {
                margin: 8px 0 0;
            }

            &__email {
                color: var(--fd-text-secondary);
            }

            &__profile-meta {
                display: flex;
                flex-wrap: wrap;
                gap: 8px;
            }

            &__chip {
                display: inline-flex;
                align-items: center;
                padding: 4px 10px;
                border-radius: 999px;
                background: color-mix(in srgb, var(--fd-color-primary-100, var(--fd-color-primary-100)) 45%, var(--fd-color-white));
                color: var(--fd-color-primary-700);
            }

            &__sections {
                display: flex;
                flex-direction: column;
                gap: 16px;
            }

            &__section {
                padding: 16px;

                h2 {
                    margin: 0 0 8px;
                }
            }

            &__placeholder {
                color: var(--fd-text-secondary);
            }

            &__no-data {
                color: var(--fd-text-secondary);
                text-align: center;
                padding: 32px;
            }
        }
    `,
})
export class ClientDashboardComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly dietologistService = inject(DietologistService);

    public readonly client = signal<ClientSummary | null>(null);
    public readonly loading = signal(true);

    public getClientTitle(client: ClientSummary): string {
        const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
        return fullName || client.email;
    }

    public constructor() {
        const clientId = this.route.snapshot.params['clientId'];
        this.dietologistService.getMyClients().subscribe({
            next: clients => {
                const found = clients.find(c => c.userId === clientId) ?? null;
                this.client.set(found);
                this.loading.set(false);
            },
            error: () => this.loading.set(false),
        });
    }

    public hasAnyPermission(): boolean {
        const p = this.client()?.permissions;
        if (!p) {
            return false;
        }
        return (
            p.shareProfile ||
            p.shareMeals ||
            p.shareStatistics ||
            p.shareWeight ||
            p.shareWaist ||
            p.shareGoals ||
            p.shareHydration ||
            p.shareFasting
        );
    }

    public goBack(): void {
        void this.router.navigate(['/dietologist']);
    }
}
