import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NoticeBannerComponent } from './notice-banner.component';

describe('NoticeBannerComponent', () => {
    let component: NoticeBannerComponent;
    let fixture: ComponentFixture<NoticeBannerComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [NoticeBannerComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(NoticeBannerComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should display title and message', () => {
        fixture.componentRef.setInput('title', 'Test Title');
        fixture.componentRef.setInput('message', 'Test Message');
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        expect(el.querySelector('.fd-notice__title')?.textContent.trim()).toBe('Test Title');
        expect(el.querySelector('.fd-notice__message')?.textContent.trim()).toBe('Test Message');
    });

    it('should show action button when actionLabel is set', () => {
        fixture.componentRef.setInput('actionLabel', 'Click Me');
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const actionBtn = el.querySelector('.fd-notice__action');
        expect(actionBtn).toBeTruthy();
        expect(actionBtn?.textContent.trim()).toBe('Click Me');
    });

    it('should not show action button when actionLabel is null', () => {
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const actionBtn = el.querySelector('.fd-notice__action');
        expect(actionBtn).toBeNull();
    });

    it('should emit action on button click', () => {
        fixture.componentRef.setInput('actionLabel', 'Retry');
        fixture.detectChanges();

        const actionSpy = vi.fn();
        component.action.subscribe(actionSpy);

        const el: HTMLElement = fixture.nativeElement;
        const actionBtn = el.querySelector<HTMLButtonElement>('.fd-notice__action');
        actionBtn?.click();

        expect(actionSpy).toHaveBeenCalledOnce();
    });

    it('should not emit action when actionLabel is null and onAction is called', () => {
        fixture.detectChanges();

        const actionSpy = vi.fn();
        component.action.subscribe(actionSpy);

        component.onAction();

        expect(actionSpy).not.toHaveBeenCalled();
    });

    it('should have showAction computed as true when actionLabel is set', () => {
        fixture.componentRef.setInput('actionLabel', 'Some Action');
        fixture.detectChanges();
        expect(component.showAction()).toBe(true);
    });

    it('should have showAction computed as false when actionLabel is null', () => {
        fixture.detectChanges();
        expect(component.showAction()).toBe(false);
    });

    describe('type variants', () => {
        const types = ['info', 'warning', 'error'] as const;

        types.forEach(type => {
            it(`should apply notice surface class for type "${type}"`, () => {
                fixture.componentRef.setInput('type', type);
                fixture.detectChanges();

                const el: HTMLElement = fixture.nativeElement;
                const notice = el.querySelector('.fd-notice');
                const toneClass = type === 'error' ? 'fd-ui-notice-surface--danger' : `fd-ui-notice-surface--${type}`;
                expect(notice?.classList.contains('fd-ui-notice-surface')).toBe(true);
                expect(notice?.classList.contains(toneClass)).toBe(true);
            });
        });
    });

    it('should default to info type', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const notice = el.querySelector('.fd-notice');
        expect(notice?.classList.contains('fd-ui-notice-surface')).toBe(true);
        expect(notice?.classList.contains('fd-ui-notice-surface--info')).toBe(true);
    });
});
