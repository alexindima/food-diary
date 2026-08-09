import type { Badge } from '../../models/gamification.data';

export type BadgeDisplay = {
    icon: string;
    nameKey: string;
    name: string;
} & Badge;

export type GamificationStatTile = {
    key: string;
    value: string;
    labelKey: string;
    icon: string;
    iconClass: string;
    accentColor: string;
};
