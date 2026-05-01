import { computed, inject, Injectable, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { GamificationService } from '../api/gamification.service';
import { Badge, GamificationData } from '../models/gamification.data';

@Injectable()
export class GamificationFacade {
    private readonly gamificationService = inject(GamificationService);
    private readonly dataResource = resource({
        loader: async (): Promise<GamificationData> => firstValueFrom(this.gamificationService.getData()),
    });

    public readonly isLoading = computed(() => this.dataResource.isLoading());
    public readonly data = computed(() => (this.dataResource.hasValue() ? this.dataResource.value() : null));

    public readonly currentStreak = computed(() => this.data()?.currentStreak ?? 0);
    public readonly longestStreak = computed(() => this.data()?.longestStreak ?? 0);
    public readonly totalMealsLogged = computed(() => this.data()?.totalMealsLogged ?? 0);
    public readonly healthScore = computed(() => this.data()?.healthScore ?? 0);
    public readonly weeklyAdherence = computed(() => Math.round((this.data()?.weeklyAdherence ?? 0) * 100));
    public readonly badges = computed(() => this.data()?.badges ?? []);
    public readonly earnedBadges = computed(() => this.badges().filter(b => b.isEarned));
    public readonly lockedBadges = computed(() => this.badges().filter(b => !b.isEarned));

    public initialize(): void {
        this.dataResource.reload();
    }

    public getBadgeIcon(badge: Badge): string {
        if (badge.category === 'streak') {
            return 'local_fire_department';
        }
        return 'restaurant';
    }
}
