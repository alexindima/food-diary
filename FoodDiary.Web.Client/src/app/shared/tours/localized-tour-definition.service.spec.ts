import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { LocalizedTourDefinitionService } from './localized-tour-definition.service';

describe('LocalizedTourDefinitionService targets', () => {
    it.each(['page-help', '#custom-help', '[data-custom="help"]', '.custom-help'])('preserves the specific target %s', target => {
        TestBed.configureTestingModule({ providers: [{ provide: TranslateService, useValue: { instant: (key: string): string => key } }] });
        const definition = TestBed.inject(LocalizedTourDefinitionService).build({
            id: 'responsive-page',
            translationRoot: 'PAGE.TOUR',
            steps: [{ id: 'help', target, titleKey: 'HELP', descriptionKey: 'HELP_TEXT', placement: 'bottom' }],
        });
        expect(definition.steps[0].target).toBe(target === 'page-help' ? '[data-tour-id="page-help"]' : target);
        expect(definition.steps[0].title).toBe('PAGE.TOUR.HELP');
    });
});
