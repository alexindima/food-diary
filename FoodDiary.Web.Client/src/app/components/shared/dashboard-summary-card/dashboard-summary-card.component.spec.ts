import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { DashboardSummaryCardComponent, NutrientBar } from './dashboard-summary-card.component';

describe('DashboardSummaryCardComponent', () => {
    let component: DashboardSummaryCardComponent;
    let fixture: ComponentFixture<DashboardSummaryCardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [DashboardSummaryCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(DashboardSummaryCardComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('keeps decorative chart svg out of the tab order', () => {
        fixture.componentRef.setInput('dailyGoal', 2000);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const svg = host.querySelector('.dashboard-summary-card__svg');
        const rings = host.querySelectorAll('.dashboard-summary-card__ring');

        expect(svg?.getAttribute('aria-hidden')).toBe('true');
        expect(svg?.getAttribute('focusable')).toBe('false');
        expect(Array.from(rings).every(ring => !ring.hasAttribute('tabindex'))).toBe(true);
    });

    describe('dailyPercent', () => {
        it('should calculate daily percent correctly', () => {
            fixture.componentRef.setInput('dailyConsumed', 1500);
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.detectChanges();
            expect(component.dailyPercent()).toBe(75);
        });

        it('should handle zero goal gracefully', () => {
            fixture.componentRef.setInput('dailyConsumed', 500);
            fixture.componentRef.setInput('dailyGoal', 0);
            fixture.detectChanges();
            expect(component.dailyPercent()).toBe(0);
        });

        it('should return 100 when consumed equals goal', () => {
            fixture.componentRef.setInput('dailyConsumed', 2000);
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.detectChanges();
            expect(component.dailyPercent()).toBe(100);
        });

        it('should allow values above 100 when consumed exceeds goal', () => {
            fixture.componentRef.setInput('dailyConsumed', 3000);
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.detectChanges();
            expect(component.dailyPercent()).toBe(150);
        });
    });

    describe('clampPercent', () => {
        it('should clamp percent to 0-120 range', () => {
            fixture.detectChanges();
            expect(component.clampPercent(150)).toBe(120);
            expect(component.clampPercent(-10)).toBe(0);
            expect(component.clampPercent(50)).toBe(50);
        });

        it('should return 0 for NaN', () => {
            fixture.detectChanges();
            expect(component.clampPercent(NaN)).toBe(0);
        });
    });

    describe('weeklyPercent', () => {
        it('should calculate weekly percent from daily goal * 7', () => {
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.componentRef.setInput('weeklyConsumed', 7000);
            fixture.detectChanges();
            expect(component.weeklyPercent()).toBe(50);
        });

        it('should use explicit weekly goal when provided', () => {
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.componentRef.setInput('weeklyGoal', 10000);
            fixture.componentRef.setInput('weeklyConsumed', 5000);
            fixture.detectChanges();
            expect(component.weeklyPercent()).toBe(50);
        });
    });

    describe('goalAction', () => {
        it('should emit goalAction', () => {
            fixture.detectChanges();
            const emitSpy = vi.fn();
            component.goalAction.subscribe(emitSpy);

            component.onGoalAction();
            expect(emitSpy).toHaveBeenCalled();
        });
    });

    describe('hasCalorieGoal', () => {
        it('should detect hasCalorieGoal when daily goal is positive', () => {
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.detectChanges();
            expect(component.showNotice()).toBe(true); // no macro goals by default
        });

        it('should show notice when no calorie goal', () => {
            fixture.componentRef.setInput('dailyGoal', 0);
            fixture.detectChanges();
            expect(component.showNotice()).toBe(true);
        });
    });

    describe('normalizedDailyGoal', () => {
        it('should normalize negative goal to 0', () => {
            fixture.componentRef.setInput('dailyGoal', -100);
            fixture.detectChanges();
            expect(component.normalizedDailyGoal()).toBe(0);
        });

        it('should keep positive goal as is', () => {
            fixture.componentRef.setInput('dailyGoal', 2500);
            fixture.detectChanges();
            expect(component.normalizedDailyGoal()).toBe(2500);
        });
    });

    describe('normalizedWeeklyGoal', () => {
        it('should derive weekly goal from daily goal when not explicitly set', () => {
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.detectChanges();
            expect(component.normalizedWeeklyGoal()).toBe(14000);
        });

        it('should use explicit weekly goal when provided', () => {
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.componentRef.setInput('weeklyGoal', 10000);
            fixture.detectChanges();
            expect(component.normalizedWeeklyGoal()).toBe(10000);
        });

        it('should return 0 when daily goal is 0 and no weekly goal', () => {
            fixture.componentRef.setInput('dailyGoal', 0);
            fixture.detectChanges();
            expect(component.normalizedWeeklyGoal()).toBe(0);
        });
    });

    describe('hover states', () => {
        it('should set daily hover when goal exists', () => {
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.detectChanges();
            component.setDailyHover(true);
            expect(component.isDailyHovered()).toBe(true);
        });

        it('should not set daily hover when goal is 0', () => {
            fixture.componentRef.setInput('dailyGoal', 0);
            fixture.detectChanges();
            component.setDailyHover(true);
            expect(component.isDailyHovered()).toBe(false);
        });

        it('should set weekly hover when goal exists', () => {
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.detectChanges();
            component.setWeeklyHover(true);
            expect(component.isWeeklyHovered()).toBe(true);
        });

        it('should not set weekly hover when goal is 0', () => {
            fixture.componentRef.setInput('dailyGoal', 0);
            fixture.detectChanges();
            component.setWeeklyHover(true);
            expect(component.isWeeklyHovered()).toBe(false);
        });
    });

    describe('showNotice', () => {
        it('should show notice when no calorie goal and no macro goals', () => {
            fixture.componentRef.setInput('dailyGoal', 0);
            fixture.componentRef.setInput('nutrientBars', null);
            fixture.detectChanges();
            expect(component.showNotice()).toBe(true);
        });

        it('should not show notice when both calorie and macro goals are set', () => {
            const bars: NutrientBar[] = [
                { id: 'protein', label: 'Protein', current: 50, target: 100, unit: 'g', colorStart: '#4dabff', colorEnd: '#2563eb' },
            ];
            fixture.componentRef.setInput('dailyGoal', 2000);
            fixture.componentRef.setInput('nutrientBars', bars);
            fixture.detectChanges();
            expect(component.showNotice()).toBe(false);
        });
    });
});
