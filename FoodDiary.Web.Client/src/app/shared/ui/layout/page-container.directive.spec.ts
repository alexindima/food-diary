import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdPageContainerDirective } from './page-container.directive';

@Component({
    imports: [FdPageContainerDirective],
    template: ' <main fdPageContainer [fullHeight]="fullHeight()">Content</main> ',
})
class PageContainerHostComponent {
    public readonly fullHeight = signal(true);
}

describe('FdPageContainerDirective', () => {
    it('should apply page container host styles', () => {
        const fixture = TestBed.createComponent(PageContainerHostComponent);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const element = host.querySelector('main') as HTMLElement;

        expect(element.classList.contains('fd-page-container')).toBe(true);
        expect(element.style.display).toBe('flex');
        expect(element.style.minHeight).toBe('100%');
        fixture.componentInstance.fullHeight.set(false);
        fixture.detectChanges();
        expect(element.style.minHeight).toBe('auto');
        expect(element.style.flexDirection).toBe('column');
        expect(element.style.gap).toBe('var(--fd-page-body-gap)');
        expect(element.style.maxWidth).toBe('var(--fd-layout-page-content-max-width)');
        expect(element.style.padding).toBe('var(--fd-page-container-padding)');
    });
});
