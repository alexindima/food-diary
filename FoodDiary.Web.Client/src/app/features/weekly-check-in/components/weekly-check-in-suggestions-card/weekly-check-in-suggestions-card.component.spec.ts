import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { WeeklyCheckInSuggestionViewModel } from '../../lib/weekly-check-in.types';
import { WeeklyCheckInSuggestionsCardComponent } from './weekly-check-in-suggestions-card.component';

describe('WeeklyCheckInSuggestionsCardComponent', () => {
    it('renders empty state when there are no suggestions', () => {
        const fixture = setupComponent([]);

        expect(getText(fixture)).toContain('WEEKLY_CHECK_IN.NO_SUGGESTIONS');
    });

    it('renders suggestions', () => {
        const fixture = setupComponent([{ key: 'ADD_PROTEIN', labelKey: 'WEEKLY_CHECK_IN.ADD_PROTEIN' }]);

        expect(getText(fixture)).toContain('WEEKLY_CHECK_IN.ADD_PROTEIN');
    });
});

function setupComponent(suggestions: WeeklyCheckInSuggestionViewModel[]): ComponentFixture<WeeklyCheckInSuggestionsCardComponent> {
    TestBed.configureTestingModule({
        imports: [WeeklyCheckInSuggestionsCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(WeeklyCheckInSuggestionsCardComponent);
    fixture.componentRef.setInput('suggestions', suggestions);
    fixture.detectChanges();
    return fixture;
}

function getText(fixture: ComponentFixture<WeeklyCheckInSuggestionsCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
