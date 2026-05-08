export function parseIntegerInput(value: string | number): number | null {
    const parsedValue = typeof value === 'number' ? value : Number.parseInt(value, 10);
    return Number.isNaN(parsedValue) ? null : parsedValue;
}
