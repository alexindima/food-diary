import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { LocalizedTourDefinitionService } from './localized-tour-definition.service';

it('uses the translation key when a localized tour label resolves to a dictionary', async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateTesting()] });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
        PAGE: { TOUR: { HELP: { SUMMARY: 'Help' }, HELP_TEXT: 'Open this page to see your diary.' } },
    });
    await firstValueFrom(translate.use('en'));

    const definition = TestBed.inject(LocalizedTourDefinitionService).build({
        id: 'responsive-page',
        translationRoot: 'PAGE.TOUR',
        steps: [{ id: 'help', target: 'page-help', titleKey: 'HELP', descriptionKey: 'HELP_TEXT', placement: 'bottom' }],
    });

    expect(definition.steps[0]).toMatchObject({
        title: 'PAGE.TOUR.HELP',
        description: 'Open this page to see your diary.',
    });
});

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
