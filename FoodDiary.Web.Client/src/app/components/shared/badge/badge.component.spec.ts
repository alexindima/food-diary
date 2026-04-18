import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BadgeComponent } from './badge.component';

describe('BadgeComponent', () => {
    let component: BadgeComponent;
    let fixture: ComponentFixture<BadgeComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [BadgeComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(BadgeComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('label', 'Test Label');
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should render label text', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const labelEl = el.querySelector('.fd-badge__label');
        expect(labelEl?.textContent?.trim()).toBe('Test Label');
    });

    it('should apply default variant class (neutral)', () => {
        fixture.detectChanges();
        expect(component.variantClass).toBe('fd-badge--neutral');
    });

    describe('variant classes', () => {
        const variants = ['primary', 'success', 'warning', 'danger', 'neutral'] as const;

        variants.forEach(variant => {
            it(`should return fd-badge--${variant} for variant "${variant}"`, () => {
                fixture.componentRef.setInput('variant', variant);
                fixture.detectChanges();
                expect(component.variantClass).toBe(`fd-badge--${variant}`);
            });

            it(`should apply fd-badge--${variant} as CSS class`, () => {
                fixture.componentRef.setInput('variant', variant);
                fixture.detectChanges();
                const el: HTMLElement = fixture.nativeElement;
                const badge = el.querySelector('.fd-badge');
                expect(badge?.classList.contains(`fd-badge--${variant}`)).toBe(true);
            });
        });
    });

    it('should show icon when icon input is provided', () => {
        fixture.componentRef.setInput('icon', 'star');
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const iconEl = el.querySelector('.fd-badge__icon');
        expect(iconEl).toBeTruthy();
    });

    it('should not show icon when icon input is not provided', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const iconEl = el.querySelector('.fd-badge__icon');
        expect(iconEl).toBeNull();
    });
});
