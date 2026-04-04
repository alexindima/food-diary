import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
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
                    <h1 class="client-dashboard__title">{{ client()!.firstName || '' }} {{ client()!.lastName || '' }}</h1>
                    <span class="client-dashboard__email">{{ client()!.email }}</span>
                }
            </div>

            @if (loading()) {
                <p>Loading client data...</p>
            } @else if (!client()) {
                <p>Client not found.</p>
            } @else {
                <div class="client-dashboard__sections">
                    @if (client()!.permissions.shareStatistics) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2>Statistics</h2>
                                <p class="client-dashboard__placeholder">Dashboard data will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareMeals) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2>Meals</h2>
                                <p class="client-dashboard__placeholder">Meal data will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareWeight) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2>Weight</h2>
                                <p class="client-dashboard__placeholder">Weight history will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareWaist) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2>Waist</h2>
                                <p class="client-dashboard__placeholder">Waist history will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareGoals) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2>Goals</h2>
                                <p class="client-dashboard__placeholder">Client goals will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (client()!.permissions.shareHydration) {
                        <fd-ui-card>
                            <div class="client-dashboard__section">
                                <h2>Hydration</h2>
                                <p class="client-dashboard__placeholder">Hydration data will be displayed here.</p>
                            </div>
                        </fd-ui-card>
                    }
                    @if (!hasAnyPermission()) {
                        <p class="client-dashboard__no-data">Client has not shared any data.</p>
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
                font-size: 24px;
                font-weight: 600;
                margin: 8px 0 0;
            }

            &__email {
                color: var(--fd-text-secondary);
                font-size: 14px;
            }

            &__sections {
                display: flex;
                flex-direction: column;
                gap: 16px;
            }

            &__section {
                padding: 16px;

                h2 {
                    font-size: 18px;
                    font-weight: 600;
                    margin: 0 0 8px;
                }
            }

            &__placeholder {
                color: var(--fd-text-secondary);
                font-size: 14px;
            }

            &__no-data {
                color: var(--fd-text-secondary);
                text-align: center;
                padding: 32px;
            }
        }
    `,
})
export class ClientDashboardComponent implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly dietologistService = inject(DietologistService);

    public readonly client = signal<ClientSummary | null>(null);
    public readonly loading = signal(true);

    public ngOnInit(): void {
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
        return p.shareMeals || p.shareStatistics || p.shareWeight || p.shareWaist || p.shareGoals || p.shareHydration;
    }

    public goBack(): void {
        void this.router.navigate(['/dietologist']);
    }
}
