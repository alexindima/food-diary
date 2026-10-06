import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { readLessonListQuery, writeLessonListQuery } from './lesson-list-query';
describe('lesson list query', () => {
    it('round trips every existing list filter and page', () => {
        const query = { page: 2, category: 'Micronutrients', difficulty: 'Intermediate', sort: 'shortest' as const, search: 'iron' };
        expect(readLessonListQuery(convertToParamMap(writeLessonListQuery(query)))).toEqual(query);
    });
    it.each(['0', '-1', '1.5', 'NaN', '10001'])('normalizes unsupported page %s and invalid options', page => {
        expect(
            readLessonListQuery(convertToParamMap({ page, category: 'unknown', difficulty: 'unknown', sort: 'unknown', search: '  ' })),
        ).toEqual({ page: 1, category: null, difficulty: null, sort: 'recommended', search: '' });
    });
});
