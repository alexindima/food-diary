import { describe, expect, it } from 'vitest';

import { matchFieldValidator } from './match-field.validator';

type MatchFieldControl = Parameters<ReturnType<typeof matchFieldValidator>>[0];

type ParentControlState = {
    get: (name: string) => { value: unknown } | null;
};

function createControlState(value: unknown, parent: ParentControlState | null = null): MatchFieldControl {
    const control = Object.create(null) as MatchFieldControl;
    Object.defineProperties(control, {
        parent: { value: parent },
        value: { value },
    });

    return control;
}

function createParentState(fields: Record<string, unknown>): ParentControlState {
    return {
        get: (name: string): { value: unknown } | null => {
            if (!Object.hasOwn(fields, name)) {
                return null;
            }

            return { value: fields[name] };
        },
    };
}

describe('matchFieldValidator', () => {
    it('should return null when fields match', () => {
        const validator = matchFieldValidator('password');
        const control = createControlState('secret', createParentState({ password: 'secret' }));

        const result = validator(control);
        expect(result).toBeNull();
    });

    it("should return error when fields don't match", () => {
        const validator = matchFieldValidator('password');
        const control = createControlState('different', createParentState({ password: 'secret' }));

        const result = validator(control);
        expect(result).toEqual({ matchField: true });
    });

    it('should handle null parent', () => {
        const validator = matchFieldValidator('other');
        const control = createControlState('value');

        const result = validator(control);
        expect(result).toBeNull();
    });

    it('should handle missing control', () => {
        const validator = matchFieldValidator('nonExistent');
        const control = createControlState('value', createParentState({}));

        const result = validator(control);
        expect(result).toEqual({ matchField: true });
    });
});
