import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { WaistSummaryCardComponent } from './waist-summary-card.component';

const LATEST_WAIST = 90.5;
const DESIRED_WAIST = 80;
const LOWER_WAIST = 88;
const BASE_WAIST = 92;
const HIGHER_WAIST = 95;
const SAME_WAIST = 90;
const POSITIVE_TREND_LABEL_DELTA = '4.0';
const NEGATIVE_TREND_LABEL_DELTA = '3.0';

let component: WaistSummaryCardComponent;
let fixture: ComponentFixture<WaistSummaryCardComponent>;

describe('WaistSummaryCardComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [WaistSummaryCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(WaistSummaryCardComponent);
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

        it('should show latest waist measurement', () => {
            fixture.componentRef.setInput('latest', LATEST_WAIST);
            fixture.detectChanges();
            expect(component.latest()).toBe(LATEST_WAIST);
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
            fixture.componentRef.setInput('desired', DESIRED_WAIST);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WAIST_CARD.GOAL', { value: DESIRED_WAIST });
            expect(component.metaText()).toBe(expected);
        });

        it('should return empty meta text when desired is null', () => {
            fixture.componentRef.setInput('desired', null);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WAIST_CARD.META_EMPTY');
            expect(component.metaText()).toBe(expected);
        });
    });
}

function registerTrendTests(): void {
    describe('trend', () => {
        it('should calculate positive trend when losing waist toward goal', () => {
            fixture.componentRef.setInput('latest', LOWER_WAIST);
            fixture.componentRef.setInput('previous', BASE_WAIST);
            fixture.componentRef.setInput('desired', DESIRED_WAIST);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('positive');
            expect(trend.label).toContain(POSITIVE_TREND_LABEL_DELTA);
        });

        it('should calculate negative trend when gaining waist away from goal', () => {
            fixture.componentRef.setInput('latest', HIGHER_WAIST);
            fixture.componentRef.setInput('previous', BASE_WAIST);
            fixture.componentRef.setInput('desired', DESIRED_WAIST);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('negative');
            expect(trend.label).toContain(NEGATIVE_TREND_LABEL_DELTA);
        });

        it('should return neutral when no change', () => {
            fixture.componentRef.setInput('latest', SAME_WAIST);
            fixture.componentRef.setInput('previous', SAME_WAIST);
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
            fixture.componentRef.setInput('latest', SAME_WAIST);
            fixture.componentRef.setInput('previous', null);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WAIST_CARD.NO_PREVIOUS');
            expect(component.trend().label).toBe(expected);
            expect(component.trend().status).toBe('neutral');
        });

        it('should return neutral status when no desired value is set', () => {
            fixture.componentRef.setInput('latest', LOWER_WAIST);
            fixture.componentRef.setInput('previous', BASE_WAIST);
            fixture.componentRef.setInput('desired', null);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('neutral');
            expect(trend.label).toContain(POSITIVE_TREND_LABEL_DELTA);
        });
    });
}
