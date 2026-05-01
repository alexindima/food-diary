import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiEntityCardComponent } from './fd-ui-entity-card.component';

@Component({
    standalone: true,
    imports: [FdUiEntityCardComponent],
    template: `
        <fd-ui-entity-card [title]="title" [meta]="meta" [imageUrl]="imageUrl">
            <p class="projected-content">Body content</p>
        </fd-ui-entity-card>
    `,
})
class TestHostComponent {
    public title = 'Test Title';
    public meta: string | undefined;
    public imageUrl: string | null = null;
}

describe('FdUiEntityCardComponent', () => {
    let fixture: ComponentFixture<TestHostComponent>;
    let hostEl: HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(TestHostComponent);
        hostEl = fixture.nativeElement;
        fixture.detectChanges();
    });

    it('should create', () => {
        const entityCard = hostEl.querySelector('fd-ui-entity-card');
        expect(entityCard).toBeTruthy();
    });

    it('should display title', () => {
        const titleEl = hostEl.querySelector('.fd-ui-entity-card__title');
        expect(titleEl?.textContent?.trim()).toBe('Test Title');
    });

    it('should display meta when provided', () => {
        expect(hostEl.querySelector('.fd-ui-entity-card__meta')).toBeNull();

        fixture.componentInstance.meta = '100 kcal';
        fixture.changeDetectorRef.markForCheck();
        fixture.detectChanges();

        const metaEl = hostEl.querySelector('.fd-ui-entity-card__meta');
        expect(metaEl?.textContent?.trim()).toBe('100 kcal');
    });

    it('should render image when imageUrl provided', () => {
        fixture.componentInstance.imageUrl = 'https://example.com/food.jpg';
        fixture.changeDetectorRef.markForCheck();
        fixture.detectChanges();

        const img = hostEl.querySelector('.fd-ui-entity-card__media img') as HTMLImageElement;
        expect(img).toBeTruthy();
        expect(img.src).toBe('https://example.com/food.jpg');
    });

    it('should project content', () => {
        const projected = hostEl.querySelector('.projected-content');
        expect(projected).toBeTruthy();
        expect(projected?.textContent?.trim()).toBe('Body content');
    });
});
