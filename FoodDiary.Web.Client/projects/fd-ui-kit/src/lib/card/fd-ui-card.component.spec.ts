import { beforeEach, describe, expect, it } from 'vitest';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiCardComponent } from './fd-ui-card.component';

@Component({
    standalone: true,
    imports: [FdUiCardComponent],
    template: '<fd-ui-card><p class="projected">Projected content</p></fd-ui-card>',
})
class CardWithContentHostComponent {}

describe('FdUiCardComponent', () => {
    let component: FdUiCardComponent;
    let fixture: ComponentFixture<FdUiCardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiCardComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiCardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have default card class', () => {
        const cardClass = component.cardClass();
        expect(cardClass).toContain('fd-ui-card');
        expect(cardClass).toContain('fd-ui-card--appearance-default');
        expect(cardClass).not.toContain('fd-ui-card--subtle');
    });

    it('should add subtle class when subtle is true', () => {
        fixture.componentRef.setInput('subtle', true);
        fixture.detectChanges();

        const cardClass = component.cardClass();
        expect(cardClass).toContain('fd-ui-card--subtle');
    });

    it('should set appearance class', () => {
        fixture.componentRef.setInput('appearance', 'product');
        fixture.detectChanges();

        const cardClass = component.cardClass();
        expect(cardClass).toContain('fd-ui-card--appearance-product');
        expect(cardClass).not.toContain('fd-ui-card--appearance-default');
    });

    it('should display title', () => {
        fixture.componentRef.setInput('title', 'Test Title');
        fixture.detectChanges();

        const titleEl = fixture.debugElement.query(By.css('.fd-ui-card__title'));
        expect(titleEl).toBeTruthy();
        expect(titleEl.nativeElement.textContent.trim()).toBe('Test Title');
    });

    it('should display meta', () => {
        fixture.componentRef.setInput('meta', '100 kcal');
        fixture.detectChanges();

        const metaEl = fixture.debugElement.query(By.css('.fd-ui-card__meta'));
        expect(metaEl).toBeTruthy();
        expect(metaEl.nativeElement.textContent.trim()).toBe('100 kcal');
    });

    it('should project content', async () => {
        const hostFixture = TestBed.createComponent(CardWithContentHostComponent);
        hostFixture.detectChanges();

        const projected = hostFixture.debugElement.query(By.css('.projected'));
        expect(projected).toBeTruthy();
        expect(projected.nativeElement.textContent.trim()).toBe('Projected content');
    });
});
