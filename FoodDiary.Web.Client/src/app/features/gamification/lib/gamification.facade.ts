import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { GamificationService } from '../api/gamification.service';
import { Badge, GamificationData } from '../models/gamification.data';

@Injectable()
export class GamificationFacade {
    private readonly gamificationService = inject(GamificationService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isLoading = signal(false);
    public readonly data = signal<GamificationData | null>(null);

    public readonly currentStreak = computed(() => this.data()?.currentStreak ?? 0);
    public readonly longestStreak = computed(() => this.data()?.longestStreak ?? 0);
    public readonly totalMealsLogged = computed(() => this.data()?.totalMealsLogged ?? 0);
    public readonly healthScore = computed(() => this.data()?.healthScore ?? 0);
    public readonly weeklyAdherence = computed(() => Math.round((this.data()?.weeklyAdherence ?? 0) * 100));
    public readonly badges = computed(() => this.data()?.badges ?? []);
    public readonly earnedBadges = computed(() => this.badges().filter(b => b.isEarned));
    public readonly lockedBadges = computed(() => this.badges().filter(b => !b.isEarned));

    public initialize(): void {
        this.isLoading.set(true);
        this.gamificationService
            .getData()
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(data => this.data.set(data));
    }

    public getBadgeIcon(badge: Badge): string {
        if (badge.category === 'streak') {
            return 'local_fire_department';
        }
        return 'restaurant';
    }
}
