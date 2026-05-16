import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { ClientSummary } from '../../../../shared/models/dietologist.data';
import { DietologistService } from '../../api/dietologist.service';
import {
    buildClientDashboardSections,
    buildClientProfileChips,
    type ClientDashboardSection,
    getClientDashboardTitle,
} from './client-dashboard-lib/client-dashboard.mapper';

@Component({
    selector: 'fd-client-dashboard',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './client-dashboard.component.html',
    styleUrls: ['./client-dashboard.component.scss'],
})
export class ClientDashboardComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly dietologistService = inject(DietologistService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly client = signal<ClientSummary | null>(null);
    public readonly loading = signal(true);
    public readonly clientTitle = computed(() => {
        const client = this.client();
        if (client === null) {
            return '';
        }

        return getClientDashboardTitle(client);
    });
    public readonly profileChips = computed(() => {
        return buildClientProfileChips(this.client());
    });
    public readonly visibleSections = computed<ClientDashboardSection[]>(() => {
        return buildClientDashboardSections(this.client());
    });
    public readonly hasAnyPermission = computed(() => {
        return this.visibleSections().length > 0;
    });

    public constructor() {
        const clientId = this.route.snapshot.paramMap.get('clientId');
        this.dietologistService
            .getMyClients()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: clients => {
                    const found = clients.find(c => c.userId === clientId) ?? null;
                    this.client.set(found);
                    this.loading.set(false);
                },
                error: () => {
                    this.loading.set(false);
                },
            });
    }

    public goBack(): void {
        void this.router.navigate(['/dietologist']);
    }
}
