const DATE_INPUT_PART_LENGTH = 2;

export function getDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = padDateInputPart(date.getMonth() + 1);
    const day = padDateInputPart(date.getDate());
    return `${year}-${month}-${day}`;
}

export function getTimeInputValue(date: Date): string {
    const hours = padDateInputPart(date.getHours());
    const minutes = padDateInputPart(date.getMinutes());
    return `${hours}:${minutes}`;
}

function padDateInputPart(value: number): string {
    return value.toString().padStart(DATE_INPUT_PART_LENGTH, '0');
}
