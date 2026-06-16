import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { patchSignalFormModel } from './signal-form-model.utils';

type TestFormModel = {
    count: number;
    name: string;
};

describe('patchSignalFormModel', () => {
    it('keeps the current model reference when patch values are unchanged', () => {
        const initial: TestFormModel = { count: 1, name: 'Apple' };
        const model = signal(initial);

        patchSignalFormModel(model, { count: 1 });

        expect(model()).toBe(initial);
    });

    it('updates only changed patch fields', () => {
        const initial: TestFormModel = { count: 1, name: 'Apple' };
        const model = signal(initial);

        patchSignalFormModel(model, { count: 2 });

        expect(model()).toEqual({ count: 2, name: 'Apple' });
        expect(model()).not.toBe(initial);
    });

    it('ignores undefined patch values', () => {
        const initial: TestFormModel = { count: 1, name: 'Apple' };
        const model = signal(initial);

        patchSignalFormModel(model, { name: undefined });

        expect(model()).toBe(initial);
    });

    it('does not depend on Object.hasOwn browser support', () => {
        const initial: TestFormModel = { count: 1, name: 'Apple' };
        const model = signal(initial);
        const originalHasOwn = Object.hasOwn;
        let patchedModel: TestFormModel | undefined;

        try {
            Object.defineProperty(Object, 'hasOwn', {
                configurable: true,
                value: undefined,
            });

            patchSignalFormModel(model, { name: 'Pear' });
            patchedModel = model();
        } finally {
            Object.defineProperty(Object, 'hasOwn', {
                configurable: true,
                value: originalHasOwn,
            });
        }

        expect(patchedModel).toEqual({ count: 1, name: 'Pear' });
    });
});
