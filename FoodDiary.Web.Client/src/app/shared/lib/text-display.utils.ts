/** Capitalize the first letter while keeping prefixes and the remaining case intact. */
export function capitalizeFirstLetter(value: string | null | undefined): string {
    return value?.replace(/\p{L}/u, letter => letter.toUpperCase()) ?? '';
}
