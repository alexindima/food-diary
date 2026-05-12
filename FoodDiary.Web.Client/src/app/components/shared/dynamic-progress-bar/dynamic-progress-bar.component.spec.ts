import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { DynamicProgressBarComponent } from './dynamic-progress-bar.component';

const MAX_PERCENT = 100;
const HALF_PERCENT = 50;
const QUARTER_PERCENT = 25;
const THIRD_PERCENT = 33;
const ONE_AND_HALF_PERCENT = 150;
const OVERFLOW_EXPECTED_POSITION = 33.33;
const HIGH_OVERFLOW_EXPECTED_POSITION = 66.67;
const LOW_CURRENT = 10;
const LOW_TEXT_CURRENT = 20;
const BELOW_HALF_CURRENT = 30;
const CURRENT_50 = 50;
const CURRENT_75 = 75;
const CURRENT_80 = 80;
const CURRENT_100 = 100;
const CURRENT_112 = 112;
const CURRENT_150 = 150;
const CURRENT_175 = 175;
const CURRENT_200 = 200;
const CURRENT_300 = 300;
const CURRENT_333 = 333;
const CURRENT_2000 = 2000;
const CURRENT_3000 = 3000;
const NEGATIVE_MAX = -10;
const MAX_200 = 200;
const MAX_1000 = 1000;

type TestContext = {
    component: () => DynamicProgressBarComponent;
    fixture: () => ComponentFixture<DynamicProgressBarComponent>;
    setInputs: (current: number, max: number) => void;
};

describe('DynamicProgressBarComponent', () => {
    let component: DynamicProgressBarComponent;
    let fixture: ComponentFixture<DynamicProgressBarComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [DynamicProgressBarComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(DynamicProgressBarComponent);
        component = fixture.componentInstance;
    });

    const context: TestContext = {
        component: () => component,
        fixture: () => fixture,
        setInputs: (current, max) => {
            fixture.componentRef.setInput('current', current);
            fixture.componentRef.setInput('max', max);
            fixture.detectChanges();
        },
    };

    it('should create', () => {
        context.setInputs(0, MAX_PERCENT);
        expect(component).toBeTruthy();
    });

    registerProgressTests(context);
    registerProgressBarWidthTests(context);
    registerBarColorTests(context);
    registerMaxPositionTests(context);
    registerTextPositionTests(context);
    registerTextColorClassTests(context);
    registerDomRenderingTests(context);
});

function registerProgressTests({ component, setInputs }: TestContext): void {
    describe('progress', () => {
        it('should calculate progress percentage', () => {
            setInputs(CURRENT_50, MAX_200);
            expect(component().progress()).toBe(QUARTER_PERCENT);
        });

        it('should return 0 when max is 0', () => {
            setInputs(MAX_PERCENT, 0);
            expect(component().progress()).toBe(0);
        });

        it('should return 0 when max is negative', () => {
            setInputs(CURRENT_50, NEGATIVE_MAX);
            expect(component().progress()).toBe(0);
        });

        it('should return 100 when current equals max', () => {
            setInputs(CURRENT_2000, CURRENT_2000);
            expect(component().progress()).toBe(MAX_PERCENT);
        });

        it('should return above 100 when current exceeds max', () => {
            setInputs(CURRENT_3000, CURRENT_2000);
            expect(component().progress()).toBe(ONE_AND_HALF_PERCENT);
        });

        it('should round the percentage', () => {
            setInputs(CURRENT_333, MAX_1000);
            expect(component().progress()).toBe(THIRD_PERCENT);
        });
    });
}

function registerProgressBarWidthTests({ component, setInputs }: TestContext): void {
    describe('progressBarWidth', () => {
        it('should calculate bar width percentage as string', () => {
            setInputs(CURRENT_50, MAX_200);
            expect(component().progressBarWidth()).toBe('25%');
        });

        it('should clamp bar width at 100%', () => {
            setInputs(CURRENT_3000, CURRENT_2000);
            expect(component().progressBarWidth()).toBe('100%');
        });

        it('should return 0% when max is 0', () => {
            setInputs(MAX_PERCENT, 0);
            expect(component().progressBarWidth()).toBe('0%');
        });

        it('should return 100% when current equals max', () => {
            setInputs(CURRENT_2000, CURRENT_2000);
            expect(component().progressBarWidth()).toBe('100%');
        });
    });
}

function registerBarColorTests({ component, setInputs }: TestContext): void {
    describe('barColor', () => {
        it('should be green-ish at low progress', () => {
            setInputs(LOW_CURRENT, MAX_PERCENT);
            expect(component().barColor()).toBe('#50a050');
        });

        it('should be brighter green at higher (but under 100%) progress', () => {
            setInputs(CURRENT_100, MAX_PERCENT);
            expect(component().barColor()).toBe('#50fa50');
        });

        it('should transition to orange between 100-125%', () => {
            setInputs(CURRENT_112, MAX_PERCENT);
            expect(component().barColor()).toBe('#ff9850');
        });

        it('should be red-ish above 125%', () => {
            setInputs(CURRENT_175, MAX_PERCENT);
            expect(component().barColor()).toBe('#ff0000');
        });

        it('should be pure green-range at 0% progress', () => {
            setInputs(0, MAX_PERCENT);
            expect(component().barColor()).toBe('#509650');
        });
    });
}

function registerMaxPositionTests({ component, setInputs }: TestContext): void {
    describe('maxPosition', () => {
        it('should return 100 when current is within max', () => {
            setInputs(CURRENT_50, MAX_PERCENT);
            expect(component().maxPosition()).toBe(MAX_PERCENT);
        });

        it('should return 100 when current equals max', () => {
            setInputs(MAX_PERCENT, MAX_PERCENT);
            expect(component().maxPosition()).toBe(MAX_PERCENT);
        });

        it('should return proportional position when current exceeds max', () => {
            setInputs(CURRENT_200, MAX_PERCENT);
            expect(component().maxPosition()).toBe(HALF_PERCENT);
        });

        it('should return 100 when max is 0', () => {
            setInputs(CURRENT_50, 0);
            expect(component().maxPosition()).toBe(MAX_PERCENT);
        });

        it('should return 100 when current is 0', () => {
            setInputs(0, MAX_PERCENT);
            expect(component().maxPosition()).toBe(MAX_PERCENT);
        });
    });
}

function registerTextPositionTests({ component, setInputs }: TestContext): void {
    describe('textPosition', () => {
        it('should be 50% when max is 0', () => {
            setInputs(CURRENT_50, 0);
            expect(component().textPosition()).toBe('50%');
        });

        it('should position text toward the right side when progress is low', () => {
            setInputs(LOW_TEXT_CURRENT, MAX_PERCENT);
            expect(component().textPosition()).toBe('60%');
        });

        it('should position text at half progress when progress is between 50-100%', () => {
            setInputs(CURRENT_80, MAX_PERCENT);
            expect(component().textPosition()).toBe('40%');
        });

        it('should position text based on maxPosition when progress is 100-200%', () => {
            setInputs(CURRENT_150, MAX_PERCENT);
            const position = parseFloat(component().textPosition());
            expect(position).toBeCloseTo(OVERFLOW_EXPECTED_POSITION, 1);
        });

        it('should use overflow formula when progress exceeds 200%', () => {
            setInputs(CURRENT_300, MAX_PERCENT);
            const position = parseFloat(component().textPosition());
            expect(position).toBeCloseTo(HIGH_OVERFLOW_EXPECTED_POSITION, 1);
        });
    });
}

function registerTextColorClassTests({ component, setInputs }: TestContext): void {
    describe('textColorClass', () => {
        it('should return text-black when progress is below 50%', () => {
            setInputs(BELOW_HALF_CURRENT, MAX_PERCENT);
            expect(component().textColorClass()).toBe('text-black');
        });

        it('should return text-white when progress is 50% or above', () => {
            setInputs(HALF_PERCENT, MAX_PERCENT);
            expect(component().textColorClass()).toBe('text-white');
        });

        it('should return text-black when progress is 0%', () => {
            setInputs(0, MAX_PERCENT);
            expect(component().textColorClass()).toBe('text-black');
        });

        it('should return text-white when overconsumption', () => {
            setInputs(CURRENT_200, MAX_PERCENT);
            expect(component().textColorClass()).toBe('text-white');
        });
    });
}

function registerDomRenderingTests({ fixture, setInputs }: TestContext): void {
    describe('DOM rendering', () => {
        it('should show overflow line when progress exceeds 100%', () => {
            setInputs(CURRENT_150, MAX_PERCENT);
            const el = fixture().nativeElement as HTMLElement;
            const overflowLine = el.querySelector('.dynamic-progress-bar-overflow-line');
            expect(overflowLine).toBeTruthy();
        });

        it('should not show overflow line when progress is at or below 100%', () => {
            setInputs(CURRENT_80, MAX_PERCENT);
            const el = fixture().nativeElement as HTMLElement;
            const overflowLine = el.querySelector('.dynamic-progress-bar-overflow-line');
            expect(overflowLine).toBeNull();
        });

        it('should display current value and percentage in bar text', () => {
            setInputs(CURRENT_75, MAX_PERCENT);
            const el = fixture().nativeElement as HTMLElement;
            const title = el.querySelector('.dynamic-progress-bar-title');
            expect(title?.textContent.trim()).toContain('75');
            expect(title?.textContent.trim()).toContain('75%');
        });
    });
}
