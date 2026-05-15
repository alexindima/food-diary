import type { Badge } from '../../models/gamification.data';

export type BadgeDisplay = {
    icon: string;
    nameKey: string;
} & Badge;

export type HealthScoreRing = {
    strokeDasharray: number;
    strokeDashoffset: number;
};

export type GamificationStatTile = {
    key: string;
    value: string;
    labelKey: string;
    icon: string;
    iconClass: string;
    accentColor: string;
};
