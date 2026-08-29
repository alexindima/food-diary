import { inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { ActiveSessionsService } from '../api/active-sessions.service';
import type { ActiveSession } from '../models/active-session.model';

@Injectable()
export class ActiveSessionsFacade {
    private readonly api = inject(ActiveSessionsService);
    public readonly sessions = signal<ActiveSession[]>([]);
    public readonly isLoading = signal(false);
    public readonly revokingId = signal<string | null>(null);
    public readonly error = signal(false);

    public load(): void {
        this.isLoading.set(true);
        this.error.set(false);
        this.api
            .getAll()
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
            )
            .subscribe({
                next: sessions => {
                    this.sessions.set(sessions);
                },
                error: () => {
                    this.error.set(true);
                },
            });
    }

    public revoke(sessionId: string): void {
        if (this.revokingId() !== null) {
            return;
        }
        this.revokingId.set(sessionId);
        this.api
            .revoke(sessionId)
            .pipe(
                finalize(() => {
                    this.revokingId.set(null);
                }),
            )
            .subscribe({
                next: () => {
                    this.sessions.update(items => items.filter(item => item.id !== sessionId));
                },
                error: () => {
                    this.error.set(true);
                },
            });
    }

    public revokeOthers(): void {
        if (this.revokingId() !== null) {
            return;
        }
        this.revokingId.set('all');
        this.api
            .revokeOthers()
            .pipe(
                finalize(() => {
                    this.revokingId.set(null);
                }),
            )
            .subscribe({
                next: () => {
                    this.sessions.update(items => items.filter(item => item.isCurrent));
                },
                error: () => {
                    this.error.set(true);
                },
            });
    }
}
