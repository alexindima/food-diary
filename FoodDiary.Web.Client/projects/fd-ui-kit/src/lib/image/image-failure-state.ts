import { computed, linkedSignal, type Signal } from '@angular/core';

export type ImageFailureState = {
    unavailable: Signal<ReadonlySet<string>>;
    markFailed: (url: string | null | undefined) => void;
};

/** Failed URLs belong to the current source set; late errors from replaced images are ignored. */
export function createImageFailureState(source: () => ReadonlyArray<string | null | undefined>): ImageFailureState {
    const sources = computed(() => source().filter((url): url is string => url !== null && url !== undefined), {
        equal: (previous, current) => previous.length === current.length && previous.every((url, index) => url === current[index]),
    });
    const unavailable = linkedSignal({
        source: sources,
        computation: (): ReadonlySet<string> => new Set(),
    });

    return {
        unavailable: unavailable.asReadonly(),
        markFailed: (url: string | null | undefined): void => {
            if (url !== null && url !== undefined && sources().includes(url)) {
                unavailable.update(current => new Set([...current, url]));
            }
        },
    };
}
