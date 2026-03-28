import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { WaistSummaryCardComponent } from './waist-summary-card.component';

describe('WaistSummaryCardComponent', () => {
    let component: WaistSummaryCardComponent;
    let fixture: ComponentFixture<WaistSummaryCardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [WaistSummaryCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(WaistSummaryCardComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should show latest waist measurement', () => {
        fixture.componentRef.setInput('latest', 90.5);
        fixture.detectChanges();
        expect(component.latest()).toBe(90.5);
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
            fixture.componentRef.setInput('desired', 80);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WAIST_CARD.GOAL', { value: 80 });
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

    describe('trend', () => {
        it('should calculate positive trend when losing waist toward goal', () => {
            fixture.componentRef.setInput('latest', 88);
            fixture.componentRef.setInput('previous', 92);
            fixture.componentRef.setInput('desired', 80);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('positive');
            expect(trend.label).toContain('4.0');
        });

        it('should calculate negative trend when gaining waist away from goal', () => {
            fixture.componentRef.setInput('latest', 95);
            fixture.componentRef.setInput('previous', 92);
            fixture.componentRef.setInput('desired', 80);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('negative');
            expect(trend.label).toContain('3.0');
        });

        it('should return neutral when no change', () => {
            fixture.componentRef.setInput('latest', 90);
            fixture.componentRef.setInput('previous', 90);
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
            fixture.componentRef.setInput('latest', 90);
            fixture.componentRef.setInput('previous', null);
            fixture.detectChanges();

            const translateService = TestBed.inject(TranslateService);
            const expected = translateService.instant('WAIST_CARD.NO_PREVIOUS');
            expect(component.trend().label).toBe(expected);
            expect(component.trend().status).toBe('neutral');
        });

        it('should return neutral status when no desired value is set', () => {
            fixture.componentRef.setInput('latest', 88);
            fixture.componentRef.setInput('previous', 92);
            fixture.componentRef.setInput('desired', null);
            fixture.detectChanges();

            const trend = component.trend();
            expect(trend.status).toBe('neutral');
            expect(trend.label).toContain('4.0');
        });
    });
});
