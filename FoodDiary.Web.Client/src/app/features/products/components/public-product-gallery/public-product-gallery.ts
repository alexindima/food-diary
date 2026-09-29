import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogService, FdUiImagePreviewDialogComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-public-product-gallery',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe],
    templateUrl: './public-product-gallery.html',
    styleUrl: './public-product-gallery.scss',
})
export class PublicProductGalleryComponent {
    private readonly dialogs = inject(FdUiDialogService);
    public readonly images = input.required<readonly string[]>();
    public readonly name = input.required<string>();
    protected readonly selected = signal(0);
    protected readonly activeIndex = computed(() => (this.selected() < this.images().length ? this.selected() : 0));
    protected readonly activeImage = computed(() => this.images()[this.activeIndex()]);

    protected preview(): void {
        this.dialogs.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            data: {
                collageImages: this.images().map(url => ({ url, alt: this.name() })),
                initialIndex: this.activeIndex(),
                alt: this.name(),
                title: this.name(),
            },
        });
    }
}
