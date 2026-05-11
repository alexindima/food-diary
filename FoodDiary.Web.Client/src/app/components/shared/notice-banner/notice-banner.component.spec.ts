import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { NoticeBannerComponent } from './notice-banner.component';

type NoticeBannerTestContext = {
    component: NoticeBannerComponent;
    el: HTMLElement;
    fixture: ComponentFixture<NoticeBannerComponent>;
};

async function setupNoticeBannerAsync(): Promise<NoticeBannerTestContext> {
    await TestBed.configureTestingModule({
        imports: [NoticeBannerComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(NoticeBannerComponent);
    const component = fixture.componentInstance;
    const el = fixture.nativeElement as HTMLElement;

    return { component, el, fixture };
}

describe('NoticeBannerComponent', () => {
    it('should create', async () => {
        const { component, fixture } = await setupNoticeBannerAsync();
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });
});

describe('NoticeBannerComponent content', () => {
    it('should display title and message', async () => {
        const { el, fixture } = await setupNoticeBannerAsync();
        fixture.componentRef.setInput('title', 'Test Title');
        fixture.componentRef.setInput('message', 'Test Message');
        fixture.detectChanges();

        expect(el.querySelector('.fd-notice__title')?.textContent.trim()).toBe('Test Title');
        expect(el.querySelector('.fd-notice__message')?.textContent.trim()).toBe('Test Message');
    });

    it('should default to info type', async () => {
        const { el, fixture } = await setupNoticeBannerAsync();
        fixture.detectChanges();

        const notice = el.querySelector('.fd-notice');
        expect(notice?.classList.contains('fd-ui-notice-surface')).toBe(true);
        expect(notice?.classList.contains('fd-ui-notice-surface--info')).toBe(true);
    });
});

describe('NoticeBannerComponent action', () => {
    it('should show action button when actionLabel is set', async () => {
        const { el, fixture } = await setupNoticeBannerAsync();
        fixture.componentRef.setInput('actionLabel', 'Click Me');
        fixture.detectChanges();

        const actionBtn = el.querySelector('.fd-notice__action');
        expect(actionBtn).toBeTruthy();
        expect(actionBtn?.textContent.trim()).toBe('Click Me');
    });

    it('should not show action button when actionLabel is null', async () => {
        const { el, fixture } = await setupNoticeBannerAsync();
        fixture.detectChanges();

        const actionBtn = el.querySelector('.fd-notice__action');
        expect(actionBtn).toBeNull();
    });

    it('should emit action on button click', async () => {
        const { component, el, fixture } = await setupNoticeBannerAsync();
        fixture.componentRef.setInput('actionLabel', 'Retry');
        fixture.detectChanges();

        const actionSpy = vi.fn();
        component.action.subscribe(actionSpy);

        const actionBtn = el.querySelector<HTMLButtonElement>('.fd-notice__action');
        actionBtn?.click();

        expect(actionSpy).toHaveBeenCalledOnce();
    });

    it('should not emit action when actionLabel is null and onAction is called', async () => {
        const { component, fixture } = await setupNoticeBannerAsync();
        fixture.detectChanges();

        const actionSpy = vi.fn();
        component.action.subscribe(actionSpy);

        component.onAction();

        expect(actionSpy).not.toHaveBeenCalled();
    });
});

describe('NoticeBannerComponent computed state', () => {
    it('should have showAction computed as true when actionLabel is set', async () => {
        const { component, fixture } = await setupNoticeBannerAsync();
        fixture.componentRef.setInput('actionLabel', 'Some Action');
        fixture.detectChanges();
        expect(component.showAction()).toBe(true);
    });

    it('should have showAction computed as false when actionLabel is null', async () => {
        const { component, fixture } = await setupNoticeBannerAsync();
        fixture.detectChanges();
        expect(component.showAction()).toBe(false);
    });
});

describe('NoticeBannerComponent type variants', () => {
    const types = ['info', 'warning', 'error'] as const;

    types.forEach(type => {
        it(`should apply notice surface class for type "${type}"`, async () => {
            const { el, fixture } = await setupNoticeBannerAsync();
            fixture.componentRef.setInput('type', type);
            fixture.detectChanges();

            const notice = el.querySelector('.fd-notice');
            const toneClass = type === 'error' ? 'fd-ui-notice-surface--danger' : `fd-ui-notice-surface--${type}`;
            expect(notice?.classList.contains('fd-ui-notice-surface')).toBe(true);
            expect(notice?.classList.contains(toneClass)).toBe(true);
        });
    });
});
