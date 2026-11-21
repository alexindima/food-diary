import type { Badge } from '../models/gamification.data';

export function badgeIcon(badge: Badge): string {
    return badge.icon ?? (badge.category === 'streak' ? 'local_fire_department' : 'restaurant');
}

export function badgeNameKey(badge: Badge): string {
    return `GAMIFICATION.BADGE_${badge.key.toUpperCase()}`;
}
