import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FD_UI_DIALOG_DATA } from '../dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from '../dialog/fd-ui-dialog-ref';
import { FdUiImagePreviewDialogComponent, type FdUiImagePreviewDialogData } from './fd-ui-image-preview-dialog';

const PHOTO_COUNT = 6;
const SWIPE_START = 100;
const SWIPE_LEFT = 10;
const SWIPE_RIGHT = 200;
const SCROLL_END_X = 110;
const SCROLL_END_Y = 220;
function setup(data: FdUiImagePreviewDialogData): {
    fixture: ComponentFixture<FdUiImagePreviewDialogComponent>;
    component: FdUiImagePreviewDialogComponent;
    host: HTMLElement;
    close: ReturnType<typeof vi.fn>;
} {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [FdUiImagePreviewDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: data },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const fixture = TestBed.createComponent(FdUiImagePreviewDialogComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, host: fixture.nativeElement as HTMLElement, close };
}
const images = [
    { url: '/first.jpg', alt: 'First photo' },
    { url: '/second.jpg', alt: 'Second photo' },
];
describe('Image preview gallery', () => {
    it('selects a photo through thumbnails and updates the highlighted pressed state', () => {
        const { fixture, host } = setup({ collageImages: images });
        const thumbnails = host.querySelectorAll<HTMLButtonElement>('.fd-ui-image-preview-dialog__thumbnail');
        expect(thumbnails).toHaveLength(images.length);
        expect(thumbnails[0].getAttribute('aria-pressed')).toBe('true');
        thumbnails[1].click();
        fixture.detectChanges();
        expect(host.querySelector<HTMLImageElement>('.fd-ui-image-preview-dialog__image')?.src).toContain('/second.jpg');
        expect(thumbnails[0].getAttribute('aria-pressed')).toBe('false');
        expect(thumbnails[1].getAttribute('aria-pressed')).toBe('true');
        expect(thumbnails[1].classList.contains('fd-ui-image-preview-dialog__thumbnail--active')).toBe(true);
    });

    it('hides arrows and thumbnails for a single gallery image', () => {
        const { host } = setup({ collageImages: [images[0]] });
        expect(host.querySelectorAll('img')).toHaveLength(1);
        expect(host.querySelector('.fd-ui-image-preview-dialog__arrow')).toBeNull();
        expect(host.querySelector('.fd-ui-image-preview-dialog__navigation')).toBeNull();
    });

    it('navigates individual originals with keyboard and wraps in both directions', () => {
        const { fixture, host } = setup({ collageImages: images });
        expect(host.querySelector('img')?.alt).toBe('First photo');
        host.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
        fixture.detectChanges();
        expect(host.querySelector('img')?.alt).toBe('Second photo');
        expect(host.querySelectorAll('.fd-ui-image-preview-dialog__thumbnail')[1].getAttribute('aria-pressed')).toBe('true');
        expect(host.textContent).toContain('2 / 2');
        host.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
        fixture.detectChanges();
        expect(host.querySelector('img')?.alt).toBe('First photo');
    });
    it('uses the original cover without gallery controls and closes through the shared dialog', () => {
        const { host, close } = setup({ imageUrl: '/cover.jpg', alt: 'Meal', collageImages: images });
        expect(host.querySelectorAll('img')).toHaveLength(1);
        expect(host.querySelector('.fd-ui-image-preview-dialog__navigation')).toBeNull();
        host.querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button')?.click();
        expect(close).toHaveBeenCalledOnce();
    });
    it('ignores blank images and retains more than four originals', () => {
        const { component } = setup({
            collageImages: [{ url: ' ' }, ...Array.from({ length: PHOTO_COUNT }, (_, i) => ({ url: `/photo-${i}.jpg` }))],
        });
        expect(component['images']).toHaveLength(PHOTO_COUNT);
    });
    it('supports horizontal swipes without interpreting vertical scroll as navigation', () => {
        const { component } = setup({ collageImages: images });
        const swipe = (x: number, y: number): void => {
            component['startSwipe']({
                touches: { length: 1, item: () => ({ clientX: SWIPE_START, clientY: SWIPE_START }) },
            } as unknown as TouchEvent);
            component['endSwipe']({ changedTouches: { item: () => ({ clientX: x, clientY: y }) } } as unknown as TouchEvent);
        };
        swipe(SWIPE_LEFT, SWIPE_START);
        expect(component['activeIndex']()).toBe(1);
        swipe(SCROLL_END_X, SCROLL_END_Y);
        expect(component['activeIndex']()).toBe(1);
        swipe(SWIPE_RIGHT, SWIPE_START);
        expect(component['activeIndex']()).toBe(0);
    });
    it('handles absent images without navigation errors', () => {
        const { component, host } = setup({});
        component['navigate'](1);
        expect(component['activeImage']()).toBeUndefined();
        expect(host.querySelector('img')).toBeNull();
    });
});
