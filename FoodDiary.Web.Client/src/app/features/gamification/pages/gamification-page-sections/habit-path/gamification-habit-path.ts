import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiIconComponent, FdUiProgressRingComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

import { LocalizedDatePipe } from '../../../../../shared/i18n/localized-date.pipe';
import {
    GamificationAchievementsDialogComponent,
    type GamificationAchievementsDialogData,
} from '../../../dialogs/gamification-achievements-dialog/gamification-achievements-dialog';
import type { Badge } from '../../../models/gamification.data';

type RewardFilter = 'all' | 'habits' | 'nutrition' | 'academy';

type ProgressGoal = {
    key: string;
    icon: string;
    current: number;
    target: number;
    percent: number;
    category: RewardFilter;
};

const WEEK_DAYS = 7;
const PERCENT_MAX = 100;
const DEFAULT_MEALS_TARGET = 50;
const RECENT_ACHIEVEMENT_LIMIT = 2;

@Component({
    selector: 'fd-gamification-habit-path',
    imports: [TranslatePipe, LocalizedDatePipe, FdUiButtonComponent, FdUiCardComponent, FdUiIconComponent, FdUiProgressRingComponent],
    templateUrl: './gamification-habit-path.html',
    styleUrl: './gamification-habit-path.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GamificationHabitPathComponent {
    private readonly dialogService = inject(FdUiDialogService);

    public readonly currentStreak = input.required<number>();
    public readonly longestStreak = input.required<number>();
    public readonly totalMealsLogged = input.required<number>();
    public readonly healthScore = input.required<number>();
    public readonly weeklyAdherence = input.required<number>();
    public readonly badges = input.required<Badge[]>();

    protected readonly activeFilter = signal<RewardFilter>('all');
    protected readonly filters: RewardFilter[] = ['all', 'habits', 'nutrition', 'academy'];
    protected readonly earnedBadges = computed(() =>
        this.badges()
            .filter(badge => badge.isEarned)
            .sort((left, right) => Date.parse(right.earnedAtUtc ?? '') - Date.parse(left.earnedAtUtc ?? '')),
    );
    protected readonly recentBadges = computed(() => this.earnedBadges().slice(0, RECENT_ACHIEVEMENT_LIMIT));
    protected readonly lockedBadges = computed(() => this.badges().filter(badge => !badge.isEarned));
    protected readonly filteredBadges = computed(() => {
        const activeFilter = this.activeFilter();
        return this.badges().filter(badge => activeFilter === 'all' || this.badgeFilter(badge) === activeFilter);
    });
    protected readonly goals = computed<ProgressGoal[]>(() => {
        const nextStreak = this.nextBadge('streak');
        const nextMeals = this.nextBadge('meals');
        const adheredDays = Math.round((this.weeklyAdherence() / PERCENT_MAX) * WEEK_DAYS);

        return [
            this.createGoal({
                key: nextStreak?.key ?? 'streak_7',
                icon: 'local_fire_department',
                current: this.currentStreak(),
                target: nextStreak?.threshold ?? WEEK_DAYS,
                category: 'habits',
            }),
            this.createGoal({
                key: nextMeals?.key ?? 'meals_50',
                icon: 'restaurant',
                current: this.totalMealsLogged(),
                target: nextMeals?.threshold ?? DEFAULT_MEALS_TARGET,
                category: 'nutrition',
            }),
            this.createGoal({ key: 'weekly_goal', icon: 'event_available', current: adheredDays, target: WEEK_DAYS, category: 'habits' }),
        ];
    });
    protected readonly nextReward = computed(
        () =>
            this.goals()
                .filter(goal => goal.current < goal.target)
                .sort((a, b) => b.percent - a.percent)[0],
    );

    protected setFilter(filter: RewardFilter): void {
        this.activeFilter.set(filter);
    }

    protected openAchievementsDialog(): void {
        this.dialogService.open<GamificationAchievementsDialogComponent, GamificationAchievementsDialogData>(
            GamificationAchievementsDialogComponent,
            {
                data: { badges: this.earnedBadges() },
                preset: 'form',
            },
        );
    }

    protected badgeIcon(badge: Badge): string {
        return badge.icon ?? (badge.category === 'streak' ? 'local_fire_department' : 'restaurant');
    }

    protected badgeNameKey(badge: Badge): string {
        return `GAMIFICATION.BADGE_${badge.key.toUpperCase()}`;
    }

    protected badgeFilter(badge: Badge): RewardFilter {
        if (badge.category === 'streak') {
            return 'habits';
        }

        return badge.category === 'academy' ? 'academy' : 'nutrition';
    }

    private nextBadge(category: string): Badge | undefined {
        return this.badges()
            .filter(badge => badge.category === category && !badge.isEarned)
            .sort((a, b) => a.threshold - b.threshold)[0];
    }

    private createGoal(goal: Omit<ProgressGoal, 'percent'>): ProgressGoal {
        return {
            ...goal,
            current: Math.min(goal.current, goal.target),
            percent: Math.min(PERCENT_MAX, Math.round((goal.current / goal.target) * PERCENT_MAX)),
        };
    }
}
