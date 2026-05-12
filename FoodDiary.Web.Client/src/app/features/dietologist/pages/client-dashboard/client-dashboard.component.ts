import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { ClientSummary } from '../../../../shared/models/dietologist.data';
import { DietologistService } from '../../api/dietologist.service';

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
    public readonly clientTitle = computed(() => {
        const client = this.client();
        if (client === null) {
            return '';
        }

        const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
        return fullName.length > 0 ? fullName : client.email;
    });
    public readonly profileChips = computed(() => {
        const client = this.client();
        if (client?.permissions.shareProfile !== true) {
            return [];
        }

        return [this.formatProfileChip(client.height, ' cm'), client.gender, client.activityLevel].filter(
            (value): value is string => value !== null && value.length > 0,
        );
    });
    public readonly visibleSections = computed<ClientDashboardSection[]>(() => {
        const permissions = this.client()?.permissions;
        if (permissions === undefined) {
            return [];
        }

        return [
            {
                isVisible: permissions.shareProfile,
                title: 'Profile',
                body: 'Client profile data will be displayed here.',
            },
            {
                isVisible: permissions.shareStatistics,
                title: 'Statistics',
                body: 'Dashboard data will be displayed here.',
            },
            {
                isVisible: permissions.shareMeals,
                title: 'Meals',
                body: 'Meal data will be displayed here.',
            },
            {
                isVisible: permissions.shareWeight,
                title: 'Weight',
                body: 'Weight history will be displayed here.',
            },
            {
                isVisible: permissions.shareWaist,
                title: 'Waist',
                body: 'Waist history will be displayed here.',
            },
            {
                isVisible: permissions.shareGoals,
                title: 'Goals',
                body: 'Client goals will be displayed here.',
            },
            {
                isVisible: permissions.shareHydration,
                title: 'Hydration',
                body: 'Hydration data will be displayed here.',
            },
            {
                isVisible: permissions.shareFasting,
                title: 'Fasting',
                body: 'Fasting data will be displayed here.',
            },
        ].filter(section => section.isVisible);
    });
    public readonly hasAnyPermission = computed(() => {
        return this.visibleSections().length > 0;
    });

    public constructor() {
        const clientId = this.route.snapshot.paramMap.get('clientId');
        this.dietologistService.getMyClients().subscribe({
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

    private formatProfileChip(value: number | null | undefined, suffix: string): string | null {
        return value === null || value === undefined ? null : `${value}${suffix}`;
    }
}

type ClientDashboardSection = {
    isVisible: boolean;
    title: string;
    body: string;
};
