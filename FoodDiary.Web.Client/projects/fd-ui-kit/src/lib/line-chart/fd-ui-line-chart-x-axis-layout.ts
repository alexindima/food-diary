export type XAxisLabelBounds = { index: number; left: number; right: number };
type XAxisPoint = { label: string; xPosition?: number };
type XAxisTick = { x: number; label: string; position: 'start' | 'middle' | 'end' };

const MAX_X_AXIS_LABELS = 14;
const MIN_X_AXIS_LABEL_GAP = 6.5;

export function resolveXAxisTicks(
    series: ReadonlyArray<{ points: ReadonlyArray<XAxisPoint | undefined> }>,
    pointX: (point: XAxisPoint | undefined, index: number, pointCount: number) => number,
    centerX: number,
): readonly XAxisTick[] {
    const points = series.find(item => item.points.length > 0)?.points;
    if (points === undefined) {
        return [];
    }

    const tick = (index: number, x: number, position: XAxisTick['position']): XAxisTick => ({
        x,
        label: points[index]?.label.trim() ?? '',
        position,
    });
    if (points.length === 1) {
        return [tick(0, centerX, 'middle')];
    }

    const lastIndex = points.length - 1;
    const lastX = pointX(points[lastIndex], lastIndex, points.length);
    let previousX = pointX(points[0], 0, points.length);
    const ticks = [tick(0, previousX, 'start')];
    const labelIntervals = Math.min(MAX_X_AXIS_LABELS, points.length) - 1;
    for (let label = 1; label < labelIntervals; label++) {
        const index = Math.round((label * lastIndex) / labelIntervals);
        const x = pointX(points[index], index, points.length);
        if (x - previousX >= MIN_X_AXIS_LABEL_GAP && lastX - x >= MIN_X_AXIS_LABEL_GAP) {
            ticks.push(tick(index, x, 'middle'));
            previousX = x;
        }
    }
    ticks.push(tick(lastIndex, lastX, 'end'));
    return ticks;
}

export function findOverlappingXAxisLabels(bounds: readonly XAxisLabelBounds[], clearance: number): ReadonlySet<number> {
    const hidden = new Set<number>();
    const first = bounds.at(0);
    const last = bounds.at(-1);
    if (first === undefined || last === undefined || bounds.length < 2) {
        return hidden;
    }

    let previousRight = first.right;
    if (first.right + clearance > last.left) {
        hidden.add(first.index);
        previousRight = Number.NEGATIVE_INFINITY;
    }

    for (const label of bounds.slice(1, -1)) {
        if (label.left < previousRight + clearance || label.right + clearance > last.left) {
            hidden.add(label.index);
        } else {
            previousRight = label.right;
        }
    }

    return hidden;
}
