export type XAxisLabelBounds = { index: number; left: number; right: number };

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
