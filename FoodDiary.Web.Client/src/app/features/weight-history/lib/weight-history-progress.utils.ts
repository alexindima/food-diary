export type WeightChangeTone = 'positive' | 'negative' | 'neutral';

export function getWeightChangeTone(change: number | null, currentWeight: number | null, desiredWeight: number | null): WeightChangeTone {
    if (change === null || change === 0 || currentWeight === null || desiredWeight === null || currentWeight === desiredWeight) {
        return 'neutral';
    }

    return Math.sign(change) === Math.sign(desiredWeight - currentWeight) ? 'positive' : 'negative';
}

export function getWeightRemainingToGoal(startWeight: number, currentWeight: number, desiredWeight: number): number {
    const goalDirection = Math.sign(desiredWeight - startWeight);
    return goalDirection === 0 ? 0 : Math.max(0, (desiredWeight - currentWeight) * goalDirection);
}
