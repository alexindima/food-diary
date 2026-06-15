import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { Badge } from '../../../models/gamification.data';
import { GamificationBadgesCardComponent } from './gamification-badges-card';

const BADGES: Badge[] = [
    { key: 'streak_3', category: 'streak', threshold: 3, isEarned: true },
    { key: 'meals_100', category: 'meals', threshold: 100, isEarned: false },
];

describe('GamificationBadgesCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GamificationBadgesCardComponent],
            providers: [provideTranslateTesting()],
        });
    });

    it('renders earned and locked badges', () => {
        const fixture = createComponent(BADGES);
        const element = getElement(fixture);

        expect(element.querySelectorAll('.gamification__badge--earned')).toHaveLength(1);
        expect(element.querySelectorAll('.gamification__badge--locked')).toHaveLength(1);
        expect(element.textContent).toContain('GAMIFICATION.BADGE_STREAK_3');
        expect(element.textContent).toContain('GAMIFICATION.BADGE_MEALS_100');
    });

    it('renders empty state when no badges are available', () => {
        const fixture = createComponent([]);
        const element = getElement(fixture);

        expect(element.querySelectorAll('.gamification__badge')).toHaveLength(0);
        expect(element.textContent).toContain('GAMIFICATION.NO_BADGES');
    });
});

function createComponent(badges: Badge[]): ComponentFixture<GamificationBadgesCardComponent> {
    const fixture = TestBed.createComponent(GamificationBadgesCardComponent);
    fixture.componentRef.setInput('badges', badges);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GamificationBadgesCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
