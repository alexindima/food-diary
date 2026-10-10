import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';

@Injectable()
export class DashboardSetupFacade {
    private readonly userService = inject(UserService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isSaving = signal(false);
    public readonly saveFailed = signal(false);

    public async dismissAsync(): Promise<void> {
        const user = this.userService.user();
        if (user === null || this.isSaving()) {
            return;
        }

        this.isSaving.set(true);
        this.saveFailed.set(false);
        try {
            const updated = await firstValueFrom(
                this.userService
                    .updateDashboardLayout({ ...user.dashboardLayout, setupHintDismissed: true })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
                { defaultValue: null },
            );
            this.saveFailed.set(updated?.dashboardLayout?.setupHintDismissed !== true);
        } catch {
            this.saveFailed.set(true);
        } finally {
            this.isSaving.set(false);
        }
    }
}
