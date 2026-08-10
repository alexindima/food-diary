import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdUiLevelIndicatorComponent } from './fd-ui-level-indicator';

describe('FdUiLevelIndicatorComponent', () => {
    it.each([
        { input: -1, expected: 0 },
        { input: 2, expected: 2 },
        { input: 8, expected: 4 },
    ])('renders $expected filled bars for an input of $input', ({ input, expected }) => {
        const fixture = TestBed.createComponent(FdUiLevelIndicatorComponent);
        fixture.componentRef.setInput('filledCount', input);
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelectorAll('.fd-ui-level-indicator__bar--filled')).toHaveLength(expected);
    });
});
