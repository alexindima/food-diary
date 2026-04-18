import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DailyProgressCardComponent } from './daily-progress-card.component';
import { TranslateModule } from '@ngx-translate/core';

describe('DailyProgressCardComponent', () => {
    let component: DailyProgressCardComponent;
    let fixture: ComponentFixture<DailyProgressCardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [DailyProgressCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(DailyProgressCardComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('date', new Date('2026-03-28'));
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    describe('hasGoal', () => {
        it('should be false when goal is 0', () => {
            fixture.componentRef.setInput('goal', 0);
            fixture.detectChanges();
            expect(component.hasGoal()).toBe(false);
        });

        it('should be false when goal is default (0)', () => {
            fixture.detectChanges();
            expect(component.hasGoal()).toBe(false);
        });

        it('should be true when goal is positive', () => {
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.hasGoal()).toBe(true);
        });
    });

    describe('progressPercent', () => {
        it('should calculate progress percentage correctly', () => {
            fixture.componentRef.setInput('consumed', 500);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.progressPercent()).toBe(25);
        });

        it('should round the percentage', () => {
            fixture.componentRef.setInput('consumed', 333);
            fixture.componentRef.setInput('goal', 1000);
            fixture.detectChanges();
            expect(component.progressPercent()).toBe(33);
        });

        it('should return 0 when goal is 0 (no division by zero)', () => {
            fixture.componentRef.setInput('consumed', 500);
            fixture.componentRef.setInput('goal', 0);
            fixture.detectChanges();
            expect(component.progressPercent()).toBe(0);
        });

        it('should clamp progress to minimum 0 when consumed is negative', () => {
            fixture.componentRef.setInput('consumed', -100);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.progressPercent()).toBe(0);
        });

        it('should allow progress above 100 when consumed exceeds goal', () => {
            fixture.componentRef.setInput('consumed', 3000);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.progressPercent()).toBe(150);
        });

        it('should return 100 when consumed equals goal', () => {
            fixture.componentRef.setInput('consumed', 2000);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.progressPercent()).toBe(100);
        });
    });

    describe('remaining', () => {
        it('should calculate remaining calories', () => {
            fixture.componentRef.setInput('consumed', 500);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.remaining()).toBe(1500);
        });

        it('should return 0 when consumed exceeds goal', () => {
            fixture.componentRef.setInput('consumed', 2500);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.remaining()).toBe(0);
        });

        it('should return null when goal is 0', () => {
            fixture.componentRef.setInput('consumed', 500);
            fixture.componentRef.setInput('goal', 0);
            fixture.detectChanges();
            expect(component.remaining()).toBeNull();
        });

        it('should return goal value when nothing consumed', () => {
            fixture.componentRef.setInput('consumed', 0);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.remaining()).toBe(2000);
        });
    });

    describe('motivationKey', () => {
        it('should return null when goal is 0', () => {
            fixture.componentRef.setInput('consumed', 500);
            fixture.componentRef.setInput('goal', 0);
            fixture.detectChanges();
            expect(component.motivationKey()).toBeNull();
        });

        it('should return NONE key when consumed is 0', () => {
            fixture.componentRef.setInput('consumed', 0);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.NONE');
        });

        it('should return NONE key when consumed is negative', () => {
            fixture.componentRef.setInput('consumed', -10);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.NONE');
        });

        it('should return P0_10 for 0-10% progress', () => {
            fixture.componentRef.setInput('consumed', 100);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P0_10');
        });

        it('should return P10_20 for 10-20% progress', () => {
            fixture.componentRef.setInput('consumed', 300);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P10_20');
        });

        it('should return P20_30 for 20-30% progress', () => {
            fixture.componentRef.setInput('consumed', 500);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P20_30');
        });

        it('should return P30_40 for 30-40% progress', () => {
            fixture.componentRef.setInput('consumed', 700);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P30_40');
        });

        it('should return P40_50 for 40-50% progress', () => {
            fixture.componentRef.setInput('consumed', 900);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P40_50');
        });

        it('should return P50_60 for 50-60% progress', () => {
            fixture.componentRef.setInput('consumed', 1100);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P50_60');
        });

        it('should return P60_70 for 60-70% progress', () => {
            fixture.componentRef.setInput('consumed', 1300);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P60_70');
        });

        it('should return P70_80 for 70-80% progress', () => {
            fixture.componentRef.setInput('consumed', 1500);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P70_80');
        });

        it('should return P80_90 for 80-90% progress', () => {
            fixture.componentRef.setInput('consumed', 1700);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P80_90');
        });

        it('should return P90_110 for 90-110% progress', () => {
            fixture.componentRef.setInput('consumed', 2000);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P90_110');
        });

        it('should return P110_200 for 110-200% progress', () => {
            fixture.componentRef.setInput('consumed', 3000);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P110_200');
        });

        it('should return ABOVE_200 for over 200% progress', () => {
            fixture.componentRef.setInput('consumed', 5000);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.ABOVE_200');
        });

        it('should return correct key at exact boundary (10%)', () => {
            fixture.componentRef.setInput('consumed', 200);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P0_10');
        });

        it('should return next key just above boundary (>10%)', () => {
            fixture.componentRef.setInput('consumed', 201);
            fixture.componentRef.setInput('goal', 2000);
            fixture.detectChanges();
            expect(component.motivationKey()).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P10_20');
        });
    });
});
