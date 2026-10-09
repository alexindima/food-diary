import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';

import type { RefreshTokenSessionId } from '../../../shared/models/semantics/entity-id';
import { ActiveSessionsService } from '../api/active-sessions.service';
import type { ActiveSession, SessionRevocation } from '../models/active-session.model';

@Injectable()
export class ActiveSessionsFacade {
    private readonly api = inject(ActiveSessionsService);
    public readonly sessions = signal<ActiveSession[]>([]);
    public readonly isLoading = signal(false);
    public readonly revocation = signal<SessionRevocation>({ kind: 'idle' });
    public readonly error = signal<'load' | 'revoke' | null>(null);
    public readonly isBusy = computed(() => this.isLoading() || this.revocation().kind !== 'idle');
    public readonly isRevokingOthers = computed(() => this.revocation().kind === 'others');

    public isRevoking(sessionId: RefreshTokenSessionId): boolean {
        const state = this.revocation();
        return state.kind === 'single' && state.sessionId === sessionId;
    }

    public load(): void {
        if (this.isBusy()) {
            return;
        }
        this.isLoading.set(true);
        this.error.set(null);
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
                    this.error.set('load');
                },
            });
    }

    public revoke(sessionId: RefreshTokenSessionId): void {
        if (this.isBusy()) {
            return;
        }
        this.error.set(null);
        this.revocation.set({ kind: 'single', sessionId });
        this.api
            .revoke(sessionId)
            .pipe(
                finalize(() => {
                    this.revocation.set({ kind: 'idle' });
                }),
            )
            .subscribe({
                next: () => {
                    this.sessions.update(items => items.filter(item => item.id !== sessionId));
                },
                error: () => {
                    this.error.set('revoke');
                },
            });
    }

    public revokeOthers(): void {
        if (this.isBusy()) {
            return;
        }
        this.error.set(null);
        this.revocation.set({ kind: 'others' });
        this.api
            .revokeOthers()
            .pipe(
                finalize(() => {
                    this.revocation.set({ kind: 'idle' });
                }),
            )
            .subscribe({
                next: () => {
                    this.sessions.update(items => items.filter(item => item.isCurrent));
                },
                error: () => {
                    this.error.set('revoke');
                },
            });
    }
}
