import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { WeightSummaryCardComponent } from './weight-summary-card.component';

const LATEST_WEIGHT = 80.5;
const DESIRED_WEIGHT = 75;
const LOWER_WEIGHT = 82;
const BASE_WEIGHT = 85;
const HIGHER_WEIGHT = 88;
const TREND_LABEL_DELTA = '3.0';

let component: WeightSummaryCardComponent;
let fixture: ComponentFixture<WeightSummaryCardComponent>;

describe('WeightSummaryCardComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [WeightSummaryCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(WeightSummaryCardComponent);
        component = fixture.componentInstance;
    });

    registerBasicTests();
    registerMetaTextTests();
    registerTrendTests();
});

function registerBasicTests(): void {
    describe('basic rendering', () => {
        it('should create', () => {
            fixture.detectChanges();
            expect(component).toBeTruthy();
        });

        it('should show latest weight', () => {
            fixture.componentRef.setInput('latest', LATEST_WEIGHT);
            fixture.detectChanges();
            expect(component.latest()).toBe(LATEST_WEIGHT);
        });

        it('should emit cardClick', () => {
            fixture.detectChanges();
            const emitSpy = vi.fn();
            component.cardClick.subscribe(emitSpy);

            component.cardClick.emit();
            expect(emitSpy).toHaveBeenCalled();
        });
    });
}

function registerMetaTextTests(): void {
    describe('metaText', () => {
        it('should return goal text when desired is set', () => {
            fixture.componentRef.setInput('desired', DESIRED_WEIGHT);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WEIGHT_CARD.GOAL', { value: DESIRED_WEIGHT });
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
}

function registerTrendTests(): void {
    describe('trend', () => {
        it('should calculate positive trend when losing weight toward goal', () => {
            fixture.componentRef.setInput('latest', LOWER_WEIGHT);
            fixture.componentRef.setInput('previous', BASE_WEIGHT);
            fixture.componentRef.setInput('desired', DESIRED_WEIGHT);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('positive');
            expect(trend.label).toContain(TREND_LABEL_DELTA);
        });

        it('should calculate negative trend when gaining weight away from goal', () => {
            fixture.componentRef.setInput('latest', HIGHER_WEIGHT);
            fixture.componentRef.setInput('previous', BASE_WEIGHT);
            fixture.componentRef.setInput('desired', DESIRED_WEIGHT);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('negative');
            expect(trend.label).toContain(TREND_LABEL_DELTA);
        });

        it('should return neutral when no change', () => {
            fixture.componentRef.setInput('latest', BASE_WEIGHT);
            fixture.componentRef.setInput('previous', BASE_WEIGHT);
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
            fixture.componentRef.setInput('latest', LATEST_WEIGHT);
            fixture.componentRef.setInput('previous', null);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WEIGHT_CARD.NO_PREVIOUS');
            expect(component.trend().label).toBe(expected);
            expect(component.trend().status).toBe('neutral');
        });

        it('should return neutral status when no desired value is set', () => {
            fixture.componentRef.setInput('latest', LOWER_WEIGHT);
            fixture.componentRef.setInput('previous', BASE_WEIGHT);
            fixture.componentRef.setInput('desired', null);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('neutral');
            expect(trend.label).toContain(TREND_LABEL_DELTA);
        });
    });
}
