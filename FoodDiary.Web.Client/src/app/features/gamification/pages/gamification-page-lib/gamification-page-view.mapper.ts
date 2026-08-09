import type { Badge } from '../../models/gamification.data';
import type { BadgeDisplay, GamificationStatTile } from './gamification-page.models';

const BADGE_STREAK_CATEGORY = 'streak';

export function buildGamificationStats(
    currentStreak: number,
    longestStreak: number,
    totalMealsLogged: number,
    weeklyAdherence: number,
): GamificationStatTile[] {
    return [
        {
            key: 'currentStreak',
            value: currentStreak.toString(),
            labelKey: 'GAMIFICATION.CURRENT_STREAK',
            icon: 'local_fire_department',
            iconClass: 'gamification__stat-icon--streak',
            accentColor: 'var(--fd-color-orange-500)',
        },
        {
            key: 'longestStreak',
            value: longestStreak.toString(),
            labelKey: 'GAMIFICATION.LONGEST_STREAK',
            icon: 'emoji_events',
            iconClass: 'gamification__stat-icon--longest',
            accentColor: 'var(--fd-color-purple-500)',
        },
        {
            key: 'totalMealsLogged',
            value: totalMealsLogged.toString(),
            labelKey: 'GAMIFICATION.TOTAL_MEALS',
            icon: 'restaurant',
            iconClass: 'gamification__stat-icon--meals',
            accentColor: 'var(--fd-color-sky-500)',
        },
        {
            key: 'weeklyAdherence',
            value: `${weeklyAdherence}%`,
            labelKey: 'GAMIFICATION.WEEKLY_ADHERENCE',
            icon: 'check_circle',
            iconClass: 'gamification__stat-icon--adherence',
            accentColor: 'var(--fd-color-green-500)',
        },
    ];
}

export function buildBadgeDisplays(badges: Badge[]): BadgeDisplay[] {
    return badges.map(badge => ({
        ...badge,
        icon: getBadgeIcon(badge),
        nameKey: `GAMIFICATION.BADGE_${badge.key.toUpperCase()}`,
    }));
}

export function filterEarnedBadges(badges: BadgeDisplay[]): BadgeDisplay[] {
    return badges.filter(badge => badge.isEarned);
}

export function filterLockedBadges(badges: BadgeDisplay[]): BadgeDisplay[] {
    return badges.filter(badge => !badge.isEarned);
}

function getBadgeIcon(badge: Badge): string {
    return badge.category === BADGE_STREAK_CATEGORY ? 'local_fire_department' : 'restaurant';
}
