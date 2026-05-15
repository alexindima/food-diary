import { computed, inject, Injectable, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { GamificationService } from '../api/gamification.service';
import type { GamificationData } from '../models/gamification.data';

@Injectable({ providedIn: 'root' })
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
    public readonly weeklyAdherence = computed(() => Math.round((this.data()?.weeklyAdherence ?? 0) * PERCENT_MULTIPLIER));
    public readonly badges = computed(() => this.data()?.badges ?? []);

    public initialize(): void {
        this.dataResource.reload();
    }
}
