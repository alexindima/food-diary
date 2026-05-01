import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { WeightSummaryCardComponent } from './weight-summary-card.component';

describe('WeightSummaryCardComponent', () => {
    let component: WeightSummaryCardComponent;
    let fixture: ComponentFixture<WeightSummaryCardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [WeightSummaryCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(WeightSummaryCardComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should show latest weight', () => {
        fixture.componentRef.setInput('latest', 80.5);
        fixture.detectChanges();
        expect(component.latest()).toBe(80.5);
    });

    it('should emit cardClick', () => {
        fixture.detectChanges();
        const emitSpy = vi.fn();
        component.cardClick.subscribe(emitSpy);

        component.cardClick.emit();
        expect(emitSpy).toHaveBeenCalled();
    });

    describe('metaText', () => {
        it('should return goal text when desired is set', () => {
            fixture.componentRef.setInput('desired', 75);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WEIGHT_CARD.GOAL', { value: 75 });
            expect(component.metaText()).toBe(expected);
        });

        it('should return empty meta text when desired is null', () => {
            fixture.componentRef.setInput('desired', null);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WEIGHT_CARD.META_EMPTY');
            expect(component.metaText()).toBe(expected);
        });
    });

    describe('trend', () => {
        it('should calculate positive trend when losing weight toward goal', () => {
            fixture.componentRef.setInput('latest', 82);
            fixture.componentRef.setInput('previous', 85);
            fixture.componentRef.setInput('desired', 75);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('positive');
            expect(trend.label).toContain('3.0');
        });

        it('should calculate negative trend when gaining weight away from goal', () => {
            fixture.componentRef.setInput('latest', 88);
            fixture.componentRef.setInput('previous', 85);
            fixture.componentRef.setInput('desired', 75);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('negative');
            expect(trend.label).toContain('3.0');
        });

        it('should return neutral when no change', () => {
            fixture.componentRef.setInput('latest', 85);
            fixture.componentRef.setInput('previous', 85);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('neutral');
        });

        it('should handle null values gracefully', () => {
            fixture.componentRef.setInput('latest', null);
            fixture.componentRef.setInput('previous', null);
            fixture.componentRef.setInput('desired', null);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('neutral');
        });

        it('should return neutral with no previous label when previous is null', () => {
            fixture.componentRef.setInput('latest', 80);
            fixture.componentRef.setInput('previous', null);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WEIGHT_CARD.NO_PREVIOUS');
            expect(component.trend().label).toBe(expected);
            expect(component.trend().status).toBe('neutral');
        });

        it('should return neutral status when no desired value is set', () => {
            fixture.componentRef.setInput('latest', 82);
            fixture.componentRef.setInput('previous', 85);
            fixture.componentRef.setInput('desired', null);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('neutral');
            expect(trend.label).toContain('3.0');
        });
    });
});
