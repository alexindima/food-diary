import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import type { ClientSummary } from '../../../../shared/models/dietologist.data';
import { DietologistService } from '../../api/dietologist.service';
import { buildClientCardViewModels } from './dietologist-clients-lib/dietologist-clients.mapper';
import type { ClientCardViewModel } from './dietologist-clients-lib/dietologist-clients.types';
import { DietologistClientsListComponent } from './dietologist-clients-list/dietologist-clients-list.component';

@Component({
    selector: 'fd-dietologist-clients-page',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, DietologistClientsListComponent],
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
        return buildClientCardViewModels(this.clients(), this.translateService.getCurrentLang());
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });

        this.dietologistService
            .getMyClients()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
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
}
