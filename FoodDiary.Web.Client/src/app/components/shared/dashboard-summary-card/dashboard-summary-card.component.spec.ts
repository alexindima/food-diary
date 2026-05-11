import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DashboardSummaryCardComponent, type NutrientBar } from './dashboard-summary-card.component';

const DAILY_GOAL = 2000;
const DAILY_CONSUMED = 1500;
const LOW_DAILY_CONSUMED = 500;
const OVER_DAILY_CONSUMED = 3000;
const POSITIVE_GOAL = 2500;
const NEGATIVE_GOAL = -100;
const DEFAULT_WEEKLY_GOAL = 14000;
const WEEKLY_GOAL = 10000;
const WEEKLY_CONSUMED = 7000;
const HALF_WEEKLY_CONSUMED = 5000;
const HALF_PERCENT = 50;
const DAILY_PERCENT = 75;
const FULL_PERCENT = 100;
const OVER_PERCENT = 150;
const CLAMPED_MAX_PERCENT = 120;
const NEGATIVE_PERCENT = -10;
const PROTEIN_CURRENT = 50;
const PROTEIN_TARGET = 100;

interface TestContext {
    component: () => DashboardSummaryCardComponent;
    fixture: () => ComponentFixture<DashboardSummaryCardComponent>;
    setInput: (name: string, value: unknown) => void;
}

describe('DashboardSummaryCardComponent', () => {
    let component: DashboardSummaryCardComponent;
    let fixture: ComponentFixture<DashboardSummaryCardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [DashboardSummaryCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(DashboardSummaryCardComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('dailyGoal', 0);
        fixture.componentRef.setInput('dailyConsumed', 0);
        fixture.componentRef.setInput('weeklyConsumed', 0);
        fixture.componentRef.setInput('weeklyGoal', null);
        fixture.componentRef.setInput('nutrientBars', null);
    });

    const context: TestContext = {
        component: () => component,
        fixture: () => fixture,
        setInput: (name, value) => {
            fixture.componentRef.setInput(name, value);
            fixture.detectChanges();
        },
    };

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('keeps decorative chart svg out of the tab order', () => {
        context.setInput('dailyGoal', DAILY_GOAL);

        const host = fixture.nativeElement as HTMLElement;
        const svg = host.querySelector('.dashboard-summary-card__svg');
        const rings = host.querySelectorAll('.dashboard-summary-card__ring');

        expect(svg?.getAttribute('aria-hidden')).toBe('true');
        expect(svg?.getAttribute('focusable')).toBe('false');
        expect(Array.from(rings).every(ring => !ring.hasAttribute('tabindex'))).toBe(true);
    });

    registerPercentTests(context);
    registerGoalActionTests(context);
    registerGoalNormalizationTests(context);
    registerHoverTests(context);
    registerNoticeTests(context);
});

function registerPercentTests(context: TestContext): void {
    describe('percents', () => {
        it('should calculate daily percent correctly', () => {
            setDailyInputs(context, DAILY_CONSUMED, DAILY_GOAL);
            expect(context.component().dailyPercent()).toBe(DAILY_PERCENT);
        });

        it('should handle zero goal gracefully', () => {
            setDailyInputs(context, LOW_DAILY_CONSUMED, 0);
            expect(context.component().dailyPercent()).toBe(0);
        });

        it('should return 100 when consumed equals goal', () => {
            setDailyInputs(context, DAILY_GOAL, DAILY_GOAL);
            expect(context.component().dailyPercent()).toBe(FULL_PERCENT);
        });

        it('should allow values above 100 when consumed exceeds goal', () => {
            setDailyInputs(context, OVER_DAILY_CONSUMED, DAILY_GOAL);
            expect(context.component().dailyPercent()).toBe(OVER_PERCENT);
        });

        registerClampPercentTests(context);
        registerWeeklyPercentTests(context);
    });
}

function registerClampPercentTests({ component, fixture }: TestContext): void {
    describe('clampPercent', () => {
        it('should clamp percent to 0-120 range', () => {
            fixture().detectChanges();
            expect(component().clampPercent(OVER_PERCENT)).toBe(CLAMPED_MAX_PERCENT);
            expect(component().clampPercent(NEGATIVE_PERCENT)).toBe(0);
            expect(component().clampPercent(HALF_PERCENT)).toBe(HALF_PERCENT);
        });

        it('should return 0 for NaN', () => {
            fixture().detectChanges();
            expect(component().clampPercent(NaN)).toBe(0);
        });
    });
}

function registerWeeklyPercentTests(context: TestContext): void {
    describe('weeklyPercent', () => {
        it('should calculate weekly percent from daily goal * 7', () => {
            context.setInput('dailyGoal', DAILY_GOAL);
            context.setInput('weeklyConsumed', WEEKLY_CONSUMED);
            expect(context.component().weeklyPercent()).toBe(HALF_PERCENT);
        });

        it('should use explicit weekly goal when provided', () => {
            context.setInput('dailyGoal', DAILY_GOAL);
            context.setInput('weeklyGoal', WEEKLY_GOAL);
            context.setInput('weeklyConsumed', HALF_WEEKLY_CONSUMED);
            expect(context.component().weeklyPercent()).toBe(HALF_PERCENT);
        });
    });
}

function registerGoalActionTests({ component, fixture }: TestContext): void {
    describe('goalAction', () => {
        it('should emit goalAction', () => {
            fixture().detectChanges();
            const emitSpy = vi.fn();
            component().goalAction.subscribe(emitSpy);

            component().onGoalAction();
            expect(emitSpy).toHaveBeenCalled();
        });
    });
}

function registerGoalNormalizationTests(context: TestContext): void {
    describe('goal normalization', () => {
        it('should detect hasCalorieGoal when daily goal is positive', () => {
            context.setInput('dailyGoal', DAILY_GOAL);
            expect(context.component().showNotice()).toBe(true);
        });

        it('should show notice when no calorie goal', () => {
            context.setInput('dailyGoal', 0);
            expect(context.component().showNotice()).toBe(true);
        });

        it('should normalize negative goal to 0', () => {
            context.setInput('dailyGoal', NEGATIVE_GOAL);
            expect(context.component().normalizedDailyGoal()).toBe(0);
        });

        it('should keep positive goal as is', () => {
            context.setInput('dailyGoal', POSITIVE_GOAL);
            expect(context.component().normalizedDailyGoal()).toBe(POSITIVE_GOAL);
        });

        registerWeeklyGoalNormalizationTests(context);
    });
}

function registerWeeklyGoalNormalizationTests(context: TestContext): void {
    describe('normalizedWeeklyGoal', () => {
        it('should derive weekly goal from daily goal when not explicitly set', () => {
            context.setInput('dailyGoal', DAILY_GOAL);
            expect(context.component().normalizedWeeklyGoal()).toBe(DEFAULT_WEEKLY_GOAL);
        });

        it('should use explicit weekly goal when provided', () => {
            context.setInput('dailyGoal', DAILY_GOAL);
            context.setInput('weeklyGoal', WEEKLY_GOAL);
            expect(context.component().normalizedWeeklyGoal()).toBe(WEEKLY_GOAL);
        });

        it('should return 0 when daily goal is 0 and no weekly goal', () => {
            context.setInput('dailyGoal', 0);
            expect(context.component().normalizedWeeklyGoal()).toBe(0);
        });
    });
}

function registerHoverTests(context: TestContext): void {
    describe('hover states', () => {
        it('should set daily hover when goal exists', () => {
            context.setInput('dailyGoal', DAILY_GOAL);
            context.component().setDailyHover(true);
            expect(context.component().isDailyHovered()).toBe(true);
        });

        it('should not set daily hover when goal is 0', () => {
            context.setInput('dailyGoal', 0);
            context.component().setDailyHover(true);
            expect(context.component().isDailyHovered()).toBe(false);
        });

        it('should set weekly hover when goal exists', () => {
            context.setInput('dailyGoal', DAILY_GOAL);
            context.component().setWeeklyHover(true);
            expect(context.component().isWeeklyHovered()).toBe(true);
        });

        it('should not set weekly hover when goal is 0', () => {
            context.setInput('dailyGoal', 0);
            context.component().setWeeklyHover(true);
            expect(context.component().isWeeklyHovered()).toBe(false);
        });
    });
}

function registerNoticeTests(context: TestContext): void {
    describe('showNotice', () => {
        it('should show notice when no calorie goal and no macro goals', () => {
            context.setInput('dailyGoal', 0);
            context.setInput('nutrientBars', null);
            expect(context.component().showNotice()).toBe(true);
        });

        it('should not show notice when both calorie and macro goals are set', () => {
            const bars: NutrientBar[] = [
                {
                    id: 'protein',
                    label: 'Protein',
                    current: PROTEIN_CURRENT,
                    target: PROTEIN_TARGET,
                    unit: 'g',
                    colorStart: '#4dabff',
                    colorEnd: '#2563eb',
                },
            ];
            context.setInput('dailyGoal', DAILY_GOAL);
            context.setInput('nutrientBars', bars);
            expect(context.component().showNotice()).toBe(false);
        });
    });
}

function setDailyInputs(context: TestContext, consumed: number, goal: number): void {
    context.setInput('dailyConsumed', consumed);
    context.setInput('dailyGoal', goal);
}
