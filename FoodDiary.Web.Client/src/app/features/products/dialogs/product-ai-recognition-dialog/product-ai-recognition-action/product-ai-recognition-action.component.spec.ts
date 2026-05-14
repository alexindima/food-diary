import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { ProductAiRecognitionActionComponent } from './product-ai-recognition-action.component';

describe('ProductAiRecognitionActionComponent', () => {
    it('emits analyze before the first analysis', () => {
        const { fixture, component } = setupComponent(false);
        let analyzeCount = 0;
        component.analyze.subscribe(() => {
            analyzeCount += 1;
        });

        fixture.debugElement.query(By.css('fd-ui-button')).triggerEventHandler('click');

        expect(getText(fixture)).toContain('PRODUCT_AI_DIALOG.ANALYZE');
        expect(analyzeCount).toBe(1);
    });

    it('emits reanalyze after analysis has completed', () => {
        const { fixture, component } = setupComponent(true);
        let reanalyzeCount = 0;
        component.reanalyze.subscribe(() => {
            reanalyzeCount += 1;
        });

        fixture.debugElement.query(By.css('fd-ui-button')).triggerEventHandler('click');

        expect(getText(fixture)).toContain('PRODUCT_AI_DIALOG.REANALYZE');
        expect(reanalyzeCount).toBe(1);
    });
});

function setupComponent(hasAnalyzed: boolean): {
    fixture: ComponentFixture<ProductAiRecognitionActionComponent>;
    component: ProductAiRecognitionActionComponent;
} {
    TestBed.configureTestingModule({
        imports: [ProductAiRecognitionActionComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(ProductAiRecognitionActionComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('hasAnalyzed', hasAnalyzed);
    fixture.componentRef.setInput('disabled', false);
    fixture.detectChanges();

    return { fixture, component };
}

function getText(fixture: ComponentFixture<ProductAiRecognitionActionComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
