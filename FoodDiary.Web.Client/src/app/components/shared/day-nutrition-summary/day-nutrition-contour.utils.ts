const CENTER = 160;
const INNER_RADIUS = 94;
const GOAL_RADIUS = 140;
const POINTS = 160;
const PERCENT = 100;
const MAX_VISUAL_PROGRESS = 2;
const OVERFLOW_SATURATION_MULTIPLIER = 10;
const OVERFLOW_LOG_SCALE = 1 / Math.log2(OVERFLOW_SATURATION_MULTIPLIER);
export const EXTREME_NUTRIENT_PERCENT = 250;
const MAX_VALLEY_DEPTH = 0.9;
const HALF_SECTOR = 0.5;
const QUARTER_TURN = Math.PI / 2;
const START_ANGLE = -Math.PI + QUARTER_TURN / 2;
const NUTRIENT_ORDER: readonly string[] = ['protein', 'fats', 'fiber', 'carbs'];

type NutrientProgress = { id: string; percent: number };

export function getDayNutrientAnchor(id: string, percent: number): { x: number; y: number } | null {
    const index = NUTRIENT_ORDER.indexOf(id);
    if (index === -1) {
        return null;
    }
    const angle = START_ANGLE + index * QUARTER_TURN;
    const radius = INNER_RADIUS + (GOAL_RADIUS - INNER_RADIUS) * normalizeProgress(percent);
    const x = CENTER + Math.cos(angle) * radius;
    const y = CENTER + Math.sin(angle) * radius;
    return { x, y };
}

/** Equal neighbours form an arc; differences deepen the shared valley with zero endpoint slopes. */
export function buildDayNutrientContour(bars: readonly NutrientProgress[]): string {
    const percentages = NUTRIENT_ORDER.map(id => {
        const percent = bars.find(bar => bar.id === id)?.percent ?? 0;
        return Number.isFinite(percent) ? Math.max(0, percent) : 0;
    });
    const values = percentages.map(normalizeProgress);
    const outer = Array.from({ length: POINTS }, (_, index) => {
        const position = (index / POINTS) * NUTRIENT_ORDER.length;
        const sector = Math.floor(position);
        const fraction = position - sector;
        const current = values[sector];
        const next = values[(sector + 1) % values.length];
        // Compare source percentages: logarithmic radius compression must not hide imbalance.
        const currentPercent = percentages[sector];
        const nextPercent = percentages[(sector + 1) % percentages.length];
        const maximum = Math.max(currentPercent, nextPercent);
        const difference = maximum > 0 ? Math.abs(currentPercent - nextPercent) / maximum : 0;
        const valley = Math.min(current, next) * (1 - MAX_VALLEY_DEPTH * difference);
        const descending = fraction < HALF_SECTOR;
        const localPosition = descending ? fraction / HALF_SECTOR : (fraction - HALF_SECTOR) / HALF_SECTOR;
        const from = descending ? current : valley;
        const to = descending ? valley : next;
        const blend = (1 - Math.cos(localPosition * Math.PI)) / 2;
        const radius = INNER_RADIUS + (GOAL_RADIUS - INNER_RADIUS) * (from + (to - from) * blend);
        return point(START_ANGLE + position * QUARTER_TURN, radius);
    });
    const inner = Array.from({ length: POINTS }, (_, index) => point(START_ANGLE + (index / POINTS) * Math.PI * 2, INNER_RADIUS));

    return `M ${outer.join(' L ')} Z M ${inner.join(' L ')} Z`;
}

function point(angle: number, radius: number): string {
    return `${(CENTER + Math.cos(angle) * radius).toFixed(2)} ${(CENTER + Math.sin(angle) * radius).toFixed(2)}`;
}

function normalizeProgress(percent: number): number {
    if (!Number.isFinite(percent)) {
        return 0;
    }
    const progress = Math.max(0, percent / PERCENT);
    // Compress excess only; keep the whole contour inside the canvas, including its markers.
    return progress <= 1 ? progress : Math.min(MAX_VISUAL_PROGRESS, 1 + OVERFLOW_LOG_SCALE * Math.log2(progress));
}
