import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiDialogComponent } from '../dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from '../dialog/fd-ui-dialog-data';

export type FdUiImagePreviewDialogData = {
    imageUrl?: string;
    collageImages?: readonly FdUiImagePreviewDialogCollageImage[];
    alt?: string;
    title?: string;
};

export type FdUiImagePreviewDialogCollageImage = {
    url: string;
    alt?: string;
};

const SWIPE_THRESHOLD = 40;

@Component({
    selector: 'fd-ui-image-preview-dialog',
    imports: [CommonModule, FdUiDialogComponent, FdUiButtonComponent, TranslatePipe],
    templateUrl: './fd-ui-image-preview-dialog.html',
    styleUrls: ['./fd-ui-image-preview-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(keydown.arrowLeft)': 'navigate(-1, $event)', '(keydown.arrowRight)': 'navigate(1, $event)' },
})
export class FdUiImagePreviewDialogComponent {
    private readonly dialogData = inject<FdUiImagePreviewDialogData>(FD_UI_DIALOG_DATA);

    protected readonly imageUrl = this.dialogData.imageUrl?.trim() ?? '';
    protected readonly images =
        this.imageUrl.length > 0
            ? [{ url: this.imageUrl, alt: this.dialogData.alt }]
            : (this.dialogData.collageImages ?? []).filter(image => image.url.trim().length > 0);
    protected readonly activeIndex = signal(0);
    protected readonly activeImage = computed(() => this.images[this.activeIndex()]);
    private touchStart: { x: number; y: number } | null = null;

    protected navigate(direction: number, event?: Event): void {
        if (this.images.length < 2) {
            return;
        }
        event?.preventDefault();
        this.activeIndex.update(index => (index + direction + this.images.length) % this.images.length);
    }

    protected startSwipe(event: TouchEvent): void {
        const touch = event.touches.item(0);
        this.touchStart = touch !== null && event.touches.length === 1 ? { x: touch.clientX, y: touch.clientY } : null;
    }

    protected endSwipe(event: TouchEvent): void {
        const start = this.touchStart;
        this.touchStart = null;
        const touch = event.changedTouches.item(0);
        if (start === null || touch === null) {
            return;
        }
        const delta = touch.clientX - start.x;
        if (Math.abs(delta) > SWIPE_THRESHOLD && Math.abs(delta) > Math.abs(touch.clientY - start.y)) {
            this.navigate(delta < 0 ? 1 : -1);
        }
    }
    protected readonly alt = this.dialogData.alt ?? '';
    protected readonly title = this.dialogData.title?.trim() ?? '';
}
