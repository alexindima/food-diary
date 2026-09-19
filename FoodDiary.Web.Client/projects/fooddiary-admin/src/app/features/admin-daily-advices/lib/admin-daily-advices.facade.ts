import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Observable } from 'rxjs';

import { AdminDailyAdvicesService } from '../api/admin-daily-advices.service';
import type { AdminDailyAdvice, AdminDailyAdvicesImportResponse, AdminDailyAdviceUpdate } from '../models/admin-daily-advice.models';
import { isDailyAdviceImport, MAX_ADVICE_IMPORT_BYTES } from './daily-advice-import';

@Injectable()
export class AdminDailyAdvicesFacade {
    private readonly api = inject(AdminDailyAdvicesService);
    private readonly destroyRef = inject(DestroyRef);
    public readonly advices = signal<AdminDailyAdvice[]>([]);
    public readonly loading = signal(false);
    public readonly loadFailed = signal(false);
    public readonly importing = signal(false);
    public readonly importError = signal<string | null>(null);
    public readonly importResult = signal<AdminDailyAdvicesImportResponse | null>(null);
    public readonly page = signal(0);

    public update(id: string, value: AdminDailyAdviceUpdate): Observable<AdminDailyAdvice> {
        return this.api.update(id, value);
    }

    public delete(id: string): Observable<void> {
        return this.api.delete(id);
    }

    public loadAdvices(): void {
        this.loading.set(true);
        this.loadFailed.set(false);
        this.api
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: items => {
                    this.advices.set(items);
                    this.page.set(0);
                    this.loading.set(false);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.loading.set(false);
                },
            });
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
