import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import type { EntityCardCollageState, EntityCardPreviewInteractionState } from '../entity-card-lib/entity-card.types';
import { EntityCardThumbComponent } from './entity-card-thumb.component';

async function setupEntityCardThumbAsync(hasPreviewImage: boolean): Promise<ComponentFixture<EntityCardThumbComponent>> {
    await TestBed.configureTestingModule({
        imports: [EntityCardThumbComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(EntityCardThumbComponent);
    fixture.componentRef.setInput('imageAlt', 'Image');
    fixture.componentRef.setInput('imageIcon', 'restaurant');
    fixture.componentRef.setInput('collage', { images: [], count: 0, hasImages: false } satisfies EntityCardCollageState);
    fixture.componentRef.setInput('hasPreviewImage', hasPreviewImage);
    fixture.componentRef.setInput('previewInteraction', {
        hint: null,
        role: null,
        tabIndex: null,
        ariaLabel: null,
    } satisfies EntityCardPreviewInteractionState);
    return fixture;
}

describe('EntityCardThumbComponent', () => {
    it('emits preview only when preview image is available', async () => {
        const fixture = await setupEntityCardThumbAsync(true);
        const component = fixture.componentInstance;
        const stopPropagationSpy = vi.fn();
        const event = { stopPropagation: stopPropagationSpy } as unknown as Event;
        const previewSpy = vi.fn();
        component.preview.subscribe(previewSpy);
        fixture.detectChanges();

        component.handlePreview(event);

        expect(stopPropagationSpy).toHaveBeenCalledOnce();
        expect(previewSpy).toHaveBeenCalledOnce();
    });

    it('does not emit preview when there is no preview image', async () => {
        const fixture = await setupEntityCardThumbAsync(false);
        const component = fixture.componentInstance;
        const previewSpy = vi.fn();
        component.preview.subscribe(previewSpy);
        fixture.detectChanges();

        component.handlePreview({ stopPropagation: vi.fn() } as unknown as Event);

        expect(previewSpy).not.toHaveBeenCalled();
    });
});
