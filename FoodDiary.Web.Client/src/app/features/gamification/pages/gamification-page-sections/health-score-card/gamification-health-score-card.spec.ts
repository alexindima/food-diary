import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { GamificationHealthScoreCardComponent } from './gamification-health-score-card';

const HEALTH_SCORE = 76;

describe('GamificationHealthScoreCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GamificationHealthScoreCardComponent],
            providers: [provideTranslateTesting()],
        });
    });

    it('renders health score and ring progress', () => {
        const fixture = createComponent();
        const element = getElement(fixture);
        const progress = element.querySelector<SVGCircleElement>('.gamification__score-progress');

        expect(element.textContent).toContain(HEALTH_SCORE.toString());
        expect(element.textContent).toContain('GAMIFICATION.HEALTH_SCORE_HINT');
        expect(progress?.style.strokeDasharray).not.toBe('');
        expect(progress?.style.strokeDashoffset).not.toBe('');
    });
});

function createComponent(): ComponentFixture<GamificationHealthScoreCardComponent> {
    const fixture = TestBed.createComponent(GamificationHealthScoreCardComponent);
    fixture.componentRef.setInput('score', HEALTH_SCORE);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GamificationHealthScoreCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
