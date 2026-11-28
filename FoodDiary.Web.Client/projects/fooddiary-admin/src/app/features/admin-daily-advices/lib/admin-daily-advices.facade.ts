import { computed, DestroyRef, inject, Injectable, linkedSignal, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type Observable, tap } from 'rxjs';

import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminDailyAdvicesService } from '../api/admin-daily-advices.service';
import type { AdminDailyAdvice, AdminDailyAdvicesImportResponse, AdminDailyAdviceUpdate } from '../models/admin-daily-advice.models';
import { isDailyAdviceImport, MAX_ADVICE_IMPORT_BYTES } from './daily-advice-import';

@Injectable()
export class AdminDailyAdvicesFacade {
    private readonly api = inject(AdminDailyAdvicesService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly reloadVersion = signal(0);
    private readonly advicesResource = rxResource({
        params: this.reloadVersion,
        stream: () => this.api.getAll(),
        defaultValue: [],
    });
    private readonly cachedAdvices = linkedSignal({
        source: this.advicesResource.snapshot,
        computation: (snapshot, previous): AdminDailyAdvice[] =>
            snapshot.status === 'resolved' ? snapshot.value : (previous?.value ?? []),
    });
    public readonly advices = this.cachedAdvices.asReadonly();
    public readonly loading = this.advicesResource.isLoading;
    public readonly loadFailed = computed(() => this.advicesResource.error() !== undefined);
    public readonly importing = signal(false);
    public readonly importError = signal<string | null>(null);
    public readonly importResult = signal<AdminDailyAdvicesImportResponse | null>(null);

    public update(id: AdminId<'daily-advice'>, value: AdminDailyAdviceUpdate): Observable<AdminDailyAdvice> {
        return this.api.update(id, value).pipe(
            tap(updated => {
                this.cachedAdvices.update(advices => advices.map(advice => (advice.id === id ? updated : advice)));
                this.loadAdvices();
            }),
        );
    }

    public delete(id: AdminId<'daily-advice'>): Observable<void> {
        return this.api.delete(id).pipe(
            tap(() => {
                this.cachedAdvices.update(advices => advices.filter(advice => advice.id !== id));
                this.loadAdvices();
            }),
        );
    }

    public loadAdvices(): void {
        this.reloadVersion.update(version => version + 1);
    }

    public async importFileAsync(file: File): Promise<void> {
        if (this.importing()) {
            return;
        }
        this.importError.set(null);
        this.importResult.set(null);
        if (file.size > MAX_ADVICE_IMPORT_BYTES) {
            this.importError.set('ADMIN_DAILY_ADVICES.FILE_TOO_LARGE');
            return;
        }
        this.importing.set(true);
        let payload: unknown;
        try {
            payload = JSON.parse(await file.text());
        } catch {
            this.importError.set('ADMIN_DAILY_ADVICES.INVALID_JSON');
            this.importing.set(false);
            return;
        }
        if (this.destroyRef.destroyed) {
            return;
        }
        if (!isDailyAdviceImport(payload)) {
            this.importError.set('ADMIN_DAILY_ADVICES.INVALID_FORMAT');
            this.importing.set(false);
            return;
        }
        this.api
            .importAdvices(payload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.importResult.set(result);
                    this.importing.set(false);
                    this.loadAdvices();
                },
                error: () => {
                    this.importError.set('ADMIN_DAILY_ADVICES.IMPORT_FAILED');
                    this.importing.set(false);
                },
            });
    }
}
