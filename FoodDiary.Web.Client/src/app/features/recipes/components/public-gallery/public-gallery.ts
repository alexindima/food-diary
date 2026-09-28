import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogService, FdUiIconComponent, FdUiImagePreviewDialogComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-public-recipe-gallery',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiIconComponent],
    templateUrl: './public-gallery.html',
    styleUrl: './public-gallery.scss',
})
export class PublicRecipeGalleryComponent {
    public readonly images = input.required<string[]>();
    public readonly name = input.required<string>();
    public readonly aspect = input<'square' | 'wide'>('square');
    protected readonly selected = signal<string | null>(null);
    protected readonly activeImage = computed(() => {
        const selected = this.selected();
        return selected !== null && this.images().includes(selected) ? selected : this.images()[0];
    });
    private readonly dialogs = inject(FdUiDialogService);

    protected preview(): void {
        this.dialogs.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            data: { collageImages: this.images().map(url => ({ url, alt: this.name() })), alt: this.name(), title: this.name() },
        });
    }
}
