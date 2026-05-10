import { computed, inject, Injectable, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { GamificationService } from '../api/gamification.service';
import { type Badge, type GamificationData } from '../models/gamification.data';

export interface BadgeDisplay extends Badge {
    icon: string;
    nameKey: string;
}

@Injectable()
export class GamificationFacade {
    private readonly healthScoreCircleCircumference = 2 * Math.PI * 90;
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
    public readonly healthScoreRing = computed(() => ({
        strokeDasharray: this.healthScoreCircleCircumference,
        strokeDashoffset: this.healthScoreCircleCircumference * (1 - this.healthScore() / 100),
    }));
    public readonly badgeDisplays = computed(() => this.badges().map(badge => this.toBadgeDisplay(badge)));
    public readonly earnedBadges = computed(() => this.badgeDisplays().filter(badge => badge.isEarned));
    public readonly lockedBadges = computed(() => this.badgeDisplays().filter(badge => !badge.isEarned));

    public initialize(): void {
        this.dataResource.reload();
    }

    private toBadgeDisplay(badge: Badge): BadgeDisplay {
        return {
            ...badge,
            icon: this.getBadgeIcon(badge),
            nameKey: `GAMIFICATION.BADGE_${badge.key.toUpperCase()}`,
        };
    }

    private getBadgeIcon(badge: Badge): string {
        if (badge.category === 'streak') {
            return 'local_fire_department';
        }
        return 'restaurant';
    }
}
