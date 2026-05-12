import { computed, inject, Injectable, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { GamificationService } from '../api/gamification.service';
import type { Badge, GamificationData } from '../models/gamification.data';

const HEALTH_SCORE_RING_RADIUS = 90;

export type BadgeDisplay = {
    icon: string;
    nameKey: string;
} & Badge;

@Injectable({ providedIn: 'root' })
export class GamificationFacade {
    private readonly healthScoreCircleCircumference = 2 * Math.PI * HEALTH_SCORE_RING_RADIUS;
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
    public readonly weeklyAdherence = computed(() => Math.round((this.data()?.weeklyAdherence ?? 0) * PERCENT_MULTIPLIER));
    public readonly badges = computed(() => this.data()?.badges ?? []);
    public readonly healthScoreRing = computed(() => ({
        strokeDasharray: this.healthScoreCircleCircumference,
        strokeDashoffset: this.healthScoreCircleCircumference * (1 - this.healthScore() / PERCENT_MULTIPLIER),
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
