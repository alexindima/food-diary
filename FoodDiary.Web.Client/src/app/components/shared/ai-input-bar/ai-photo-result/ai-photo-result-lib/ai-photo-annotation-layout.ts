import type { AiPhotoAnnotation, AiPhotoConnectorPoint } from './ai-photo-result.types';

type Point = AiPhotoConnectorPoint;

type Rect = {
    x: number;
    y: number;
    width: number;
    height: number;
};

type LayoutCandidate = {
    card: Rect;
    connector: readonly [Point, Point];
    baseCost: number;
};

type SearchState = {
    assignments: ReadonlyMap<string, LayoutCandidate>;
    occupiedSlots: ReadonlySet<number>;
    cost: number;
};

export type AiPhotoLayoutMetrics = {
    cardOverlaps: number;
    connectorCrossings: number;
    connectorCardIntersections: number;
    coveredProducts: number;
    connectorProductProximity: number;
    connectorLength: number;
    score: number;
};

const FRAME_SIZE = 100;
const CARD_WIDTH = 28;
const CARD_HEIGHT = 15;
const PORTRAIT_CARD_WIDTH = 68;
const PORTRAIT_STAGE_LEFT = -72;
const PORTRAIT_STAGE_RIGHT = 172;
const PORTRAIT_STAGE_PADDING = 4;
const PORTRAIT_LEFT_CARD_X = PORTRAIT_STAGE_LEFT + PORTRAIT_STAGE_PADDING;
const PORTRAIT_RIGHT_CARD_X = PORTRAIT_STAGE_RIGHT - PORTRAIT_STAGE_PADDING - PORTRAIT_CARD_WIDTH;
const PORTRAIT_TOGGLE_RESERVED_AREA: Rect = { x: 130, y: 0, width: 42, height: 18 };
const CARD_EDGE_GAP = 2;
const SIDE_SLOT_COUNT = 5;
const HORIZONTAL_SLOT_COUNT = 3;
const SIDE_SLOT_STEP = (FRAME_SIZE - CARD_HEIGHT - CARD_EDGE_GAP * 2) / (SIDE_SLOT_COUNT - 1);
const HORIZONTAL_SLOT_STEP = (FRAME_SIZE - CARD_WIDTH - CARD_EDGE_GAP * 2) / (HORIZONTAL_SLOT_COUNT - 1);
const RIGHT_CARD_X = FRAME_SIZE - CARD_WIDTH - CARD_EDGE_GAP;
const BOTTOM_CARD_Y = FRAME_SIZE - CARD_HEIGHT - CARD_EDGE_GAP;
const BEAM_WIDTH = 2500;
const PRODUCT_PROTECTION_RADIUS = 6;
const CONNECTOR_PRODUCT_RADIUS = 3;
const EPSILON = 0.0001;

const WEIGHT_CARD_OVERLAP = 100_000;
const WEIGHT_COVERED_PRODUCT = 1_000_000;
const WEIGHT_CONNECTOR_CROSSING = 30_000;
const WEIGHT_CONNECTOR_CARD_INTERSECTION = 20_000;
const WEIGHT_CONNECTOR_PRODUCT_PROXIMITY = 4_000;
const WEIGHT_DISTANCE = 1;
const WEIGHT_SIDE_PREFERENCE = 0.35;

export function optimizeAiPhotoAnnotationLayout(
    annotations: readonly AiPhotoAnnotation[],
    allowCardsOutsidePhoto = false,
): AiPhotoAnnotation[] {
    if (annotations.length === 0) {
        return [];
    }

    const slots = createPerimeterSlots(allowCardsOutsidePhoto);
    const ordered = [...annotations].sort(compareByConstraint);
    let beam: SearchState[] = [{ assignments: new Map(), occupiedSlots: new Set(), cost: 0 }];

    for (const annotation of ordered) {
        const next: SearchState[] = [];
        for (const state of beam) {
            for (const [slotIndex, slot] of slots.entries()) {
                if (state.occupiedSlots.has(slotIndex)) {
                    continue;
                }

                const candidate = createCandidate(annotation, slot, annotations, allowCardsOutsidePhoto);
                const pairCost = calculatePairCost(candidate, state.assignments);
                const assignments = new Map(state.assignments);
                assignments.set(annotation.id, candidate);
                const occupiedSlots = new Set(state.occupiedSlots);
                occupiedSlots.add(slotIndex);
                next.push({
                    assignments,
                    occupiedSlots,
                    cost: state.cost + candidate.baseCost + pairCost,
                });
            }
        }

        next.sort((left, right) => left.cost - right.cost);
        beam = next.slice(0, BEAM_WIDTH);
    }

    const best = beam[0];
    // The search can only be empty when the recognition response exceeds the finite slot set.
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- Runtime guard for oversized provider responses.
    if (best === undefined) {
        return [...annotations];
    }

    return annotations.map(annotation => {
        const candidate = best.assignments.get(annotation.id);
        if (candidate === undefined) {
            return annotation;
        }

        return {
            ...annotation,
            cardX: candidate.card.x,
            cardY: candidate.card.y,
            cardWidth: candidate.card.width,
            cardHeight: candidate.card.height,
            connectorPoints: candidate.connector,
            connectorPath: candidate.connector.map(point => `${point.x},${point.y}`).join(' '),
        };
    });
}

export function evaluateAiPhotoAnnotationLayout(annotations: readonly AiPhotoAnnotation[]): AiPhotoLayoutMetrics {
    const pairMetrics = evaluatePairs(annotations);
    let coveredProducts = 0;
    let connectorProductProximity = 0;
    let connectorLength = 0;

    for (const annotation of annotations) {
        const card = annotationRect(annotation);
        const connector = annotationConnector(annotation);
        connectorLength += distance(connector[0], connector[1]);

        for (const product of annotations) {
            const productPoint = annotationPoint(product);
            if (pointInsideRect(productPoint, expandRect(card, PRODUCT_PROTECTION_RADIUS))) {
                coveredProducts++;
            }
            if (product.id !== annotation.id && distanceToSegment(productPoint, connector[0], connector[1]) < CONNECTOR_PRODUCT_RADIUS) {
                connectorProductProximity++;
            }
        }
    }

    const score =
        pairMetrics.cardOverlaps * WEIGHT_CARD_OVERLAP +
        coveredProducts * WEIGHT_COVERED_PRODUCT +
        pairMetrics.connectorCrossings * WEIGHT_CONNECTOR_CROSSING +
        pairMetrics.connectorCardIntersections * WEIGHT_CONNECTOR_CARD_INTERSECTION +
        connectorProductProximity * WEIGHT_CONNECTOR_PRODUCT_PROXIMITY +
        connectorLength * WEIGHT_DISTANCE;

    return {
        ...pairMetrics,
        coveredProducts,
        connectorProductProximity,
        connectorLength,
        score,
    };
}

function evaluatePairs(
    annotations: readonly AiPhotoAnnotation[],
): Pick<AiPhotoLayoutMetrics, 'cardOverlaps' | 'connectorCrossings' | 'connectorCardIntersections'> {
    let cardOverlaps = 0;
    let connectorCrossings = 0;
    let connectorCardIntersections = 0;
    for (const [index, annotation] of annotations.entries()) {
        const card = annotationRect(annotation);
        const connector = annotationConnector(annotation);
        for (const other of annotations.slice(index + 1)) {
            const otherCard = annotationRect(other);
            const otherConnector = annotationConnector(other);
            cardOverlaps += rectsOverlap(card, otherCard) ? 1 : 0;
            connectorCrossings += segmentsIntersect(connector[0], connector[1], otherConnector[0], otherConnector[1]) ? 1 : 0;
            connectorCardIntersections += segmentIntersectsRect(connector[0], connector[1], otherCard) ? 1 : 0;
            connectorCardIntersections += segmentIntersectsRect(otherConnector[0], otherConnector[1], card) ? 1 : 0;
        }
    }
    return { cardOverlaps, connectorCrossings, connectorCardIntersections };
}

function createPerimeterSlots(allowCardsOutsidePhoto: boolean): Rect[] {
    const slots: Rect[] = [];
    for (let index = 0; index < SIDE_SLOT_COUNT; index++) {
        const y = CARD_EDGE_GAP + index * SIDE_SLOT_STEP;
        slots.push({
            x: allowCardsOutsidePhoto ? PORTRAIT_LEFT_CARD_X : CARD_EDGE_GAP,
            y,
            width: allowCardsOutsidePhoto ? PORTRAIT_CARD_WIDTH : CARD_WIDTH,
            height: CARD_HEIGHT,
        });
        slots.push({
            x: allowCardsOutsidePhoto ? PORTRAIT_RIGHT_CARD_X : RIGHT_CARD_X,
            y,
            width: allowCardsOutsidePhoto ? PORTRAIT_CARD_WIDTH : CARD_WIDTH,
            height: CARD_HEIGHT,
        });
    }
    if (allowCardsOutsidePhoto) {
        return slots;
    }
    for (let index = 0; index < HORIZONTAL_SLOT_COUNT; index++) {
        const x = CARD_EDGE_GAP + index * HORIZONTAL_SLOT_STEP;
        slots.push({ x, y: CARD_EDGE_GAP, width: CARD_WIDTH, height: CARD_HEIGHT });
        slots.push({ x, y: BOTTOM_CARD_Y, width: CARD_WIDTH, height: CARD_HEIGHT });
    }
    return slots;
}

function createCandidate(
    annotation: AiPhotoAnnotation,
    card: Rect,
    annotations: readonly AiPhotoAnnotation[],
    reservePortraitToggle: boolean,
): LayoutCandidate {
    const center = { x: annotation.centerX, y: annotation.centerY };
    const anchor = nearestEdgeCenter(center, card);
    const connector: readonly [Point, Point] = [center, anchor];
    const coveredProducts = annotations.filter(product =>
        pointInsideRect(annotationPoint(product), expandRect(card, PRODUCT_PROTECTION_RADIUS)),
    ).length;
    const proximityCount = annotations.filter(
        product => product.id !== annotation.id && distanceToSegment(annotationPoint(product), center, anchor) < CONNECTOR_PRODUCT_RADIUS,
    ).length;
    const sidePreference = angularDifference(center, rectCenter(card));
    const connectorLength = distance(center, anchor);
    const reservedAreaCost = reservePortraitToggle && rectsOverlap(card, PORTRAIT_TOGGLE_RESERVED_AREA) ? WEIGHT_CARD_OVERLAP : 0;

    return {
        card,
        connector,
        baseCost:
            coveredProducts * WEIGHT_COVERED_PRODUCT +
            proximityCount * WEIGHT_CONNECTOR_PRODUCT_PROXIMITY +
            connectorLength * WEIGHT_DISTANCE +
            sidePreference * WEIGHT_SIDE_PREFERENCE +
            reservedAreaCost,
    };
}

function calculatePairCost(candidate: LayoutCandidate, assignments: ReadonlyMap<string, LayoutCandidate>): number {
    let cost = 0;
    for (const existing of assignments.values()) {
        if (rectsOverlap(candidate.card, existing.card)) {
            cost += WEIGHT_CARD_OVERLAP;
        }
        if (segmentsIntersect(candidate.connector[0], candidate.connector[1], existing.connector[0], existing.connector[1])) {
            cost += WEIGHT_CONNECTOR_CROSSING;
        }
        if (segmentIntersectsRect(candidate.connector[0], candidate.connector[1], existing.card)) {
            cost += WEIGHT_CONNECTOR_CARD_INTERSECTION;
        }
        if (segmentIntersectsRect(existing.connector[0], existing.connector[1], candidate.card)) {
            cost += WEIGHT_CONNECTOR_CARD_INTERSECTION;
        }
    }

    return cost;
}

function compareByConstraint(left: AiPhotoAnnotation, right: AiPhotoAnnotation): number {
    const leftEdgeDistance = distanceToFrameEdge(annotationPoint(left));
    const rightEdgeDistance = distanceToFrameEdge(annotationPoint(right));
    const distanceDifference = rightEdgeDistance - leftEdgeDistance;
    return distanceDifference === 0 ? left.id.localeCompare(right.id) : distanceDifference;
}

function distanceToFrameEdge(point: Point): number {
    return Math.min(point.x, point.y, FRAME_SIZE - point.x, FRAME_SIZE - point.y);
}

function annotationRect(annotation: AiPhotoAnnotation): Rect {
    return {
        x: annotation.cardX,
        y: annotation.cardY,
        width: annotation.cardWidth,
        height: annotation.cardHeight,
    };
}

function annotationPoint(annotation: AiPhotoAnnotation): Point {
    return { x: annotation.centerX, y: annotation.centerY };
}

function annotationConnector(annotation: AiPhotoAnnotation): readonly [Point, Point] {
    return annotation.connectorPoints;
}

function nearestEdgeCenter(point: Point, rect: Rect): Point {
    const edgeCenters: readonly Point[] = [
        { x: rect.x + rect.width / 2, y: rect.y },
        { x: rect.x + rect.width, y: rect.y + rect.height / 2 },
        { x: rect.x + rect.width / 2, y: rect.y + rect.height },
        { x: rect.x, y: rect.y + rect.height / 2 },
    ];
    return edgeCenters.reduce((nearest, candidate) =>
        squaredDistance(point, candidate) < squaredDistance(point, nearest) ? candidate : nearest,
    );
}

function rectCenter(rect: Rect): Point {
    return { x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 };
}

function expandRect(rect: Rect, amount: number): Rect {
    return {
        x: rect.x - amount,
        y: rect.y - amount,
        width: rect.width + amount * 2,
        height: rect.height + amount * 2,
    };
}

function pointInsideRect(point: Point, rect: Rect): boolean {
    return point.x >= rect.x && point.x <= rect.x + rect.width && point.y >= rect.y && point.y <= rect.y + rect.height;
}

function rectsOverlap(left: Rect, right: Rect): boolean {
    return (
        left.x < right.x + right.width && left.x + left.width > right.x && left.y < right.y + right.height && left.y + left.height > right.y
    );
}

function segmentIntersectsRect(start: Point, end: Point, rect: Rect): boolean {
    if (pointInsideRect(start, rect) || pointInsideRect(end, rect)) {
        return true;
    }
    const topLeft = { x: rect.x, y: rect.y };
    const topRight = { x: rect.x + rect.width, y: rect.y };
    const bottomLeft = { x: rect.x, y: rect.y + rect.height };
    const bottomRight = { x: rect.x + rect.width, y: rect.y + rect.height };
    return (
        segmentsIntersect(start, end, topLeft, topRight) ||
        segmentsIntersect(start, end, topRight, bottomRight) ||
        segmentsIntersect(start, end, bottomRight, bottomLeft) ||
        segmentsIntersect(start, end, bottomLeft, topLeft)
    );
}

function segmentsIntersect(firstStart: Point, firstEnd: Point, secondStart: Point, secondEnd: Point): boolean {
    const firstOrientation = orientation(firstStart, firstEnd, secondStart);
    const secondOrientation = orientation(firstStart, firstEnd, secondEnd);
    const thirdOrientation = orientation(secondStart, secondEnd, firstStart);
    const fourthOrientation = orientation(secondStart, secondEnd, firstEnd);
    return firstOrientation * secondOrientation < -EPSILON && thirdOrientation * fourthOrientation < -EPSILON;
}

function orientation(start: Point, end: Point, point: Point): number {
    return (end.x - start.x) * (point.y - start.y) - (end.y - start.y) * (point.x - start.x);
}

function distanceToSegment(point: Point, start: Point, end: Point): number {
    const segmentLengthSquared = squaredDistance(start, end);
    if (segmentLengthSquared <= EPSILON) {
        return distance(point, start);
    }
    const projection = ((point.x - start.x) * (end.x - start.x) + (point.y - start.y) * (end.y - start.y)) / segmentLengthSquared;
    const ratio = clamp(projection, 0, 1);
    return distance(point, {
        x: start.x + ratio * (end.x - start.x),
        y: start.y + ratio * (end.y - start.y),
    });
}

function angularDifference(first: Point, second: Point): number {
    const center = FRAME_SIZE / 2;
    const firstAngle = Math.atan2(first.y - center, first.x - center);
    const secondAngle = Math.atan2(second.y - center, second.x - center);
    const rawDifference = Math.abs(firstAngle - secondAngle);
    return Math.min(rawDifference, Math.PI * 2 - rawDifference);
}

function distance(first: Point, second: Point): number {
    return Math.sqrt(squaredDistance(first, second));
}

function squaredDistance(first: Point, second: Point): number {
    const x = first.x - second.x;
    const y = first.y - second.y;
    return x * x + y * y;
}

function clamp(value: number, minimum: number, maximum: number): number {
    return Math.min(Math.max(value, minimum), maximum);
}
