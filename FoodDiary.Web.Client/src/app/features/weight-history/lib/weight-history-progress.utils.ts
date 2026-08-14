export type WeightChangeTone = 'positive' | 'negative' | 'neutral';

export function getWeightChangeTone(change: number | null, currentWeight: number | null, desiredWeightKg: number | null): WeightChangeTone {
    if (change === null || change === 0 || currentWeight === null || desiredWeightKg === null || currentWeight === desiredWeightKg) {
        return 'neutral';
    }

    return Math.sign(change) === Math.sign(desiredWeightKg - currentWeight) ? 'positive' : 'negative';
}

export function getWeightRemainingToGoal(startWeightKg: number, currentWeight: number, desiredWeightKg: number): number {
    const goalDirection = Math.sign(desiredWeightKg - startWeightKg);
    return goalDirection === 0 ? 0 : Math.max(0, (desiredWeightKg - currentWeight) * goalDirection);
}
