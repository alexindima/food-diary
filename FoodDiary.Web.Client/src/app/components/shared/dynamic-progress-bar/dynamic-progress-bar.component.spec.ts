import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { DynamicProgressBarComponent } from './dynamic-progress-bar.component';

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

    function setInputs(current: number, max: number): void {
        fixture.componentRef.setInput('current', current);
        fixture.componentRef.setInput('max', max);
        fixture.detectChanges();
    }

    it('should create', () => {
        setInputs(0, 100);
        expect(component).toBeTruthy();
    });

    describe('progress', () => {
        it('should calculate progress percentage', () => {
            setInputs(50, 200);
            expect(component.progress()).toBe(25);
        });

        it('should return 0 when max is 0', () => {
            setInputs(100, 0);
            expect(component.progress()).toBe(0);
        });

        it('should return 0 when max is negative', () => {
            setInputs(50, -10);
            expect(component.progress()).toBe(0);
        });

        it('should return 100 when current equals max', () => {
            setInputs(2000, 2000);
            expect(component.progress()).toBe(100);
        });

        it('should return above 100 when current exceeds max', () => {
            setInputs(3000, 2000);
            expect(component.progress()).toBe(150);
        });

        it('should round the percentage', () => {
            setInputs(333, 1000);
            expect(component.progress()).toBe(33);
        });
    });

    describe('progressBarWidth', () => {
        it('should calculate bar width percentage as string', () => {
            setInputs(50, 200);
            expect(component.progressBarWidth()).toBe('25%');
        });

        it('should clamp bar width at 100%', () => {
            setInputs(3000, 2000);
            expect(component.progressBarWidth()).toBe('100%');
        });

        it('should return 0% when max is 0', () => {
            setInputs(100, 0);
            expect(component.progressBarWidth()).toBe('0%');
        });

        it('should return 100% when current equals max', () => {
            setInputs(2000, 2000);
            expect(component.progressBarWidth()).toBe('100%');
        });
    });

    describe('barColor', () => {
        it('should be green-ish at low progress', () => {
            setInputs(10, 100);
            const color = component.barColor();
            // At 10%, greenIntensity = 10, so green channel = 160
            expect(color).toBe('#50a050');
        });

        it('should be brighter green at higher (but under 100%) progress', () => {
            setInputs(100, 100);
            const color = component.barColor();
            // At 100%, greenIntensity = 100, so green channel = 250
            expect(color).toBe('#50fa50');
        });

        it('should transition to orange between 100-125%', () => {
            setInputs(112, 100);
            const color = component.barColor();
            // progress = 112, orangeIntensity = round((12/25)*100) = 48
            // green channel = 200 - 48 = 152
            expect(color).toBe('#ff9850');
        });

        it('should be red-ish above 125%', () => {
            setInputs(175, 100);
            const color = component.barColor();
            expect(color).toBe('#ff0000');
        });

        it('should be pure green-range at 0% progress', () => {
            setInputs(0, 100);
            const color = component.barColor();
            // greenIntensity = 0, green channel = 150
            expect(color).toBe('#509650');
        });
    });

    describe('maxPosition', () => {
        it('should return 100 when current is within max', () => {
            setInputs(50, 100);
            expect(component.maxPosition()).toBe(100);
        });

        it('should return 100 when current equals max', () => {
            setInputs(100, 100);
            expect(component.maxPosition()).toBe(100);
        });

        it('should return proportional position when current exceeds max', () => {
            setInputs(200, 100);
            // (100 / 200) * 100 = 50
            expect(component.maxPosition()).toBe(50);
        });

        it('should return 100 when max is 0', () => {
            setInputs(50, 0);
            expect(component.maxPosition()).toBe(100);
        });

        it('should return 100 when current is 0', () => {
            setInputs(0, 100);
            expect(component.maxPosition()).toBe(100);
        });
    });

    describe('textPosition', () => {
        it('should be 50% when max is 0', () => {
            setInputs(50, 0);
            expect(component.textPosition()).toBe('50%');
        });

        it('should position text toward the right side when progress is low', () => {
            setInputs(20, 100);
            // progress = 20, < 50: position = 100 - ((100-20)/2) = 100 - 40 = 60
            expect(component.textPosition()).toBe('60%');
        });

        it('should position text at half progress when progress is between 50-100%', () => {
            setInputs(80, 100);
            // progress = 80, >= 50 and <= 100: position = min(80/2, 50) = 40
            expect(component.textPosition()).toBe('40%');
        });

        it('should position text based on maxPosition when progress is 100-200%', () => {
            setInputs(150, 100);
            // progress = 150, > 100 and <= 200: position = maxPosition/2
            // maxPosition = (100/150)*100 = 66.67
            // position = 66.67 / 2 = 33.33
            const position = parseFloat(component.textPosition());
            expect(position).toBeCloseTo(33.33, 1);
        });

        it('should use overflow formula when progress exceeds 200%', () => {
            setInputs(300, 100);
            // progress = 300, > 200: position = 100 - ((100 - (100/300)*100) / 2)
            // = 100 - ((100 - 33.33) / 2) = 100 - 33.33 = 66.67
            const position = parseFloat(component.textPosition());
            expect(position).toBeCloseTo(66.67, 1);
        });
    });

    describe('textColorClass', () => {
        it('should return text-black when progress is below 50%', () => {
            setInputs(30, 100);
            expect(component.textColorClass()).toBe('text-black');
        });

        it('should return text-white when progress is 50% or above', () => {
            setInputs(50, 100);
            expect(component.textColorClass()).toBe('text-white');
        });

        it('should return text-black when progress is 0%', () => {
            setInputs(0, 100);
            expect(component.textColorClass()).toBe('text-black');
        });

        it('should return text-white when overconsumption', () => {
            setInputs(200, 100);
            expect(component.textColorClass()).toBe('text-white');
        });
    });

    describe('DOM rendering', () => {
        it('should show overflow line when progress exceeds 100%', () => {
            setInputs(150, 100);
            const el: HTMLElement = fixture.nativeElement;
            const overflowLine = el.querySelector('.dynamic-progress-bar-overflow-line');
            expect(overflowLine).toBeTruthy();
        });

        it('should not show overflow line when progress is at or below 100%', () => {
            setInputs(80, 100);
            const el: HTMLElement = fixture.nativeElement;
            const overflowLine = el.querySelector('.dynamic-progress-bar-overflow-line');
            expect(overflowLine).toBeNull();
        });

        it('should display current value and percentage in bar text', () => {
            setInputs(75, 100);
            const el: HTMLElement = fixture.nativeElement;
            const title = el.querySelector('.dynamic-progress-bar-title');
            expect(title?.textContent?.trim()).toContain('75');
            expect(title?.textContent?.trim()).toContain('75%');
        });
    });
});
