import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import type { ClientSummary } from '../../../../shared/models/dietologist.data';
import { DietologistFacade } from '../../lib/dietologist.facade';
import { buildClientCardViewModels } from './dietologist-clients-lib/dietologist-clients.mapper';
import type { ClientCardViewModel } from './dietologist-clients-lib/dietologist-clients.types';
import { DietologistClientsListComponent } from './dietologist-clients-list/dietologist-clients-list';

@Component({
    selector: 'fd-dietologist-clients-page',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, DietologistClientsListComponent],
    templateUrl: './dietologist-clients-page.html',
    styleUrls: ['./dietologist-clients-page.scss'],
})
export class DietologistClientsPageComponent {
    private readonly dietologistFacade = inject(DietologistFacade);
    private readonly router = inject(Router);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly languageVersion = signal(0);

    protected readonly clients = signal<ClientSummary[]>([]);
    protected readonly loading = signal(true);
    protected readonly clientItems = computed<ClientCardViewModel[]>(() => {
        this.languageVersion();
        return buildClientCardViewModels(this.clients(), this.translateService.getCurrentLang());
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });

        this.dietologistFacade
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

    protected openClient(client: ClientSummary): void {
        void this.router.navigate(['/dietologist', 'clients', client.userId]);
    }
}
