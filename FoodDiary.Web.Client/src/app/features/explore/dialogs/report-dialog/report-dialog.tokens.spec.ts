import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { REPORT_REASON_MAX_LENGTH } from './report-dialog.tokens';

const EXPECTED_REPORT_REASON_MAX_LENGTH = 1_000;

describe('report dialog tokens', () => {
    it('provides the default report reason max length', () => {
        expect(TestBed.inject(REPORT_REASON_MAX_LENGTH)).toBe(EXPECTED_REPORT_REASON_MAX_LENGTH);
    });
});
