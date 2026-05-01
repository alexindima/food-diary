import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { DietologistService } from '../../api/dietologist.service';
import { ClientSummary } from '../../models/dietologist.data';

@Component({
    selector: 'fd-client-dashboard',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './client-dashboard.component.html',
    styleUrls: ['./client-dashboard.component.scss'],
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
