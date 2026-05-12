import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { DailyProgressCardComponent } from './daily-progress-card.component';

const TEST_DATE = '2026-03-28';
const DAILY_GOAL = 2000;
const ROUNDING_GOAL = 1000;
const CONSUMED_100 = 100;
const CONSUMED_200 = 200;
const CONSUMED_201 = 201;
const CONSUMED_300 = 300;
const CONSUMED_333 = 333;
const CONSUMED_500 = 500;
const CONSUMED_700 = 700;
const CONSUMED_900 = 900;
const CONSUMED_1100 = 1100;
const CONSUMED_1300 = 1300;
const CONSUMED_1500 = 1500;
const CONSUMED_1700 = 1700;
const CONSUMED_2500 = 2500;
const CONSUMED_3000 = 3000;
const CONSUMED_5000 = 5000;
const NEGATIVE_CONSUMED = -100;
const SMALL_NEGATIVE_CONSUMED = -10;
const QUARTER_PROGRESS = 25;
const THIRD_PROGRESS = 33;
const FULL_PROGRESS = 100;
const OVER_GOAL_PROGRESS = 150;

type TestContext = {
    component: () => DailyProgressCardComponent;
    fixture: () => ComponentFixture<DailyProgressCardComponent>;
    setInput: (name: string, value: unknown) => void;
};

describe('DailyProgressCardComponent', () => {
    let component: DailyProgressCardComponent;
    let fixture: ComponentFixture<DailyProgressCardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [DailyProgressCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(DailyProgressCardComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('date', new Date(TEST_DATE));
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

    registerHasGoalTests(context);
    registerProgressPercentTests(context);
    registerRemainingTests(context);
    registerMotivationKeyTests(context);
});

function registerHasGoalTests({ component, fixture, setInput }: TestContext): void {
    describe('hasGoal', () => {
        it('should be false when goal is 0', () => {
            setInput('goal', 0);
            expect(component().hasGoal()).toBe(false);
        });

        it('should be false when goal is default (0)', () => {
            fixture().detectChanges();
            expect(component().hasGoal()).toBe(false);
        });

        it('should be true when goal is positive', () => {
            setInput('goal', DAILY_GOAL);
            expect(component().hasGoal()).toBe(true);
        });
    });
}

function registerProgressPercentTests({ component, setInput }: TestContext): void {
    describe('progressPercent', () => {
        it('should calculate progress percentage correctly', () => {
            setProgressInputs(setInput, CONSUMED_500, DAILY_GOAL);
            expect(component().progressPercent()).toBe(QUARTER_PROGRESS);
        });

        it('should round the percentage', () => {
            setProgressInputs(setInput, CONSUMED_333, ROUNDING_GOAL);
            expect(component().progressPercent()).toBe(THIRD_PROGRESS);
        });

        it('should return 0 when goal is 0 (no division by zero)', () => {
            setProgressInputs(setInput, CONSUMED_500, 0);
            expect(component().progressPercent()).toBe(0);
        });

        it('should clamp progress to minimum 0 when consumed is negative', () => {
            setProgressInputs(setInput, NEGATIVE_CONSUMED, DAILY_GOAL);
            expect(component().progressPercent()).toBe(0);
        });

        it('should allow progress above 100 when consumed exceeds goal', () => {
            setProgressInputs(setInput, CONSUMED_3000, DAILY_GOAL);
            expect(component().progressPercent()).toBe(OVER_GOAL_PROGRESS);
        });

        it('should return 100 when consumed equals goal', () => {
            setProgressInputs(setInput, DAILY_GOAL, DAILY_GOAL);
            expect(component().progressPercent()).toBe(FULL_PROGRESS);
        });
    });
}

function registerRemainingTests({ component, setInput }: TestContext): void {
    describe('remaining', () => {
        it('should calculate remaining calories', () => {
            setProgressInputs(setInput, CONSUMED_500, DAILY_GOAL);
            expect(component().remaining()).toBe(CONSUMED_1500);
        });

        it('should return 0 when consumed exceeds goal', () => {
            setProgressInputs(setInput, CONSUMED_2500, DAILY_GOAL);
            expect(component().remaining()).toBe(0);
        });

        it('should return null when goal is 0', () => {
            setProgressInputs(setInput, CONSUMED_500, 0);
            expect(component().remaining()).toBeNull();
        });

        it('should return goal value when nothing consumed', () => {
            setProgressInputs(setInput, 0, DAILY_GOAL);
            expect(component().remaining()).toBe(DAILY_GOAL);
        });
    });
}

function registerMotivationKeyTests(context: TestContext): void {
    describe('motivationKey', () => {
        it('should return null when goal is 0', () => {
            setProgressInputs(context.setInput, CONSUMED_500, 0);
            expect(context.component().motivationKey()).toBeNull();
        });

        it('should return NONE key when consumed is 0', () => {
            expectMotivationKey(context, 0, 'DAILY_PROGRESS_CARD.MOTIVATION.NONE');
        });

        it('should return NONE key when consumed is negative', () => {
            expectMotivationKey(context, SMALL_NEGATIVE_CONSUMED, 'DAILY_PROGRESS_CARD.MOTIVATION.NONE');
        });

        registerProgressMotivationRangeTests(context);
    });
}

function registerProgressMotivationRangeTests(context: TestContext): void {
    const cases: Array<[string, number, string]> = [
        ['P0_10 for 0-10% progress', CONSUMED_100, 'DAILY_PROGRESS_CARD.MOTIVATION.P0_10'],
        ['P10_20 for 10-20% progress', CONSUMED_300, 'DAILY_PROGRESS_CARD.MOTIVATION.P10_20'],
        ['P20_30 for 20-30% progress', CONSUMED_500, 'DAILY_PROGRESS_CARD.MOTIVATION.P20_30'],
        ['P30_40 for 30-40% progress', CONSUMED_700, 'DAILY_PROGRESS_CARD.MOTIVATION.P30_40'],
        ['P40_50 for 40-50% progress', CONSUMED_900, 'DAILY_PROGRESS_CARD.MOTIVATION.P40_50'],
        ['P50_60 for 50-60% progress', CONSUMED_1100, 'DAILY_PROGRESS_CARD.MOTIVATION.P50_60'],
        ['P60_70 for 60-70% progress', CONSUMED_1300, 'DAILY_PROGRESS_CARD.MOTIVATION.P60_70'],
        ['P70_80 for 70-80% progress', CONSUMED_1500, 'DAILY_PROGRESS_CARD.MOTIVATION.P70_80'],
        ['P80_90 for 80-90% progress', CONSUMED_1700, 'DAILY_PROGRESS_CARD.MOTIVATION.P80_90'],
        ['P90_110 for 90-110% progress', DAILY_GOAL, 'DAILY_PROGRESS_CARD.MOTIVATION.P90_110'],
        ['P110_200 for 110-200% progress', CONSUMED_3000, 'DAILY_PROGRESS_CARD.MOTIVATION.P110_200'],
        ['ABOVE_200 for over 200% progress', CONSUMED_5000, 'DAILY_PROGRESS_CARD.MOTIVATION.ABOVE_200'],
        ['correct key at exact boundary (10%)', CONSUMED_200, 'DAILY_PROGRESS_CARD.MOTIVATION.P0_10'],
        ['next key just above boundary (>10%)', CONSUMED_201, 'DAILY_PROGRESS_CARD.MOTIVATION.P10_20'],
    ];

    it.each(cases)('should return %s', (_label, consumed, expectedKey) => {
        expectMotivationKey(context, consumed, expectedKey);
    });
}

function expectMotivationKey({ component, setInput }: TestContext, consumed: number, expectedKey: string): void {
    setProgressInputs(setInput, consumed, DAILY_GOAL);
    expect(component().motivationKey()).toBe(expectedKey);
}

function setProgressInputs(setInput: TestContext['setInput'], consumed: number, goal: number): void {
    setInput('consumed', consumed);
    setInput('goal', goal);
}
