import { signal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { GamificationFacade } from '../../lib/gamification.facade';
import type { Badge } from '../../models/gamification.data';
import { GamificationPageComponent } from './gamification-page.component';

const CURRENT_STREAK = 3;
const LONGEST_STREAK = 12;
const TOTAL_MEALS_LOGGED = 48;
const HEALTH_SCORE = 76;
const WEEKLY_ADHERENCE = 86;
const BADGES: Badge[] = [{ key: 'streak_3', category: 'streak', threshold: 3, isEarned: true }];

describe('GamificationPageComponent', () => {
    let facade: GamificationFacadeMock;

    beforeEach(() => {
        facade = createFacadeMock();
        TestBed.configureTestingModule({
            imports: [GamificationPageComponent, TranslateModule.forRoot()],
        }).overrideComponent(GamificationPageComponent, {
            set: {
                providers: [{ provide: GamificationFacade, useValue: facade }],
            },
        });
    });

    it('initializes facade and renders page sections', () => {
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(facade.initialize).toHaveBeenCalled();
        expect(element.textContent).toContain('GAMIFICATION.TITLE');
        expect(element.textContent).toContain(CURRENT_STREAK.toString());
        expect(element.textContent).toContain(HEALTH_SCORE.toString());
        expect(element.textContent).toContain('GAMIFICATION.BADGE_STREAK_3');
    });

    it('renders loading state', () => {
        facade.isLoading.set(true);
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(element.textContent).toContain('GAMIFICATION.LOADING');
        expect(element.querySelector('fd-gamification-stats-grid')).toBeNull();
    });
});

function createComponent(): ComponentFixture<GamificationPageComponent> {
    const fixture = TestBed.createComponent(GamificationPageComponent);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GamificationPageComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

type GamificationFacadeMock = {
    isLoading: ReturnType<typeof signal<boolean>>;
    currentStreak: ReturnType<typeof signal<number>>;
    longestStreak: ReturnType<typeof signal<number>>;
    totalMealsLogged: ReturnType<typeof signal<number>>;
    healthScore: ReturnType<typeof signal<number>>;
    weeklyAdherence: ReturnType<typeof signal<number>>;
    badges: ReturnType<typeof signal<Badge[]>>;
    initialize: ReturnType<typeof vi.fn>;
};

function createFacadeMock(): GamificationFacadeMock {
    return {
        isLoading: signal(false),
        currentStreak: signal(CURRENT_STREAK),
        longestStreak: signal(LONGEST_STREAK),
        totalMealsLogged: signal(TOTAL_MEALS_LOGGED),
        healthScore: signal(HEALTH_SCORE),
        weeklyAdherence: signal(WEEKLY_ADHERENCE),
        badges: signal(BADGES),
        initialize: vi.fn(),
    };
}
