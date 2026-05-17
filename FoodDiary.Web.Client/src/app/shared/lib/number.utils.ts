export function parseIntegerInput(value: string | number): number | null {
    const parsedValue = typeof value === 'number' ? value : Number.parseInt(value, 10);
    return Number.isNaN(parsedValue) ? null : parsedValue;
}

export function parseDecimalInput(value: string | number | null | undefined): number | null {
    if (value === null || value === undefined) {
        return null;
    }

    const parsedValue = typeof value === 'number' ? value : Number(value.replace(',', '.'));
    return Number.isNaN(parsedValue) ? null : parsedValue;
}
