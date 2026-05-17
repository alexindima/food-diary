import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdPageContainerDirective } from './page-container.directive';

@Component({
    imports: [FdPageContainerDirective],
    template: ' <main fdPageContainer>Content</main> ',
})
class PageContainerHostComponent {}

describe('FdPageContainerDirective', () => {
    it('should apply page container host styles', () => {
        const fixture = TestBed.createComponent(PageContainerHostComponent);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const element = host.querySelector('main') as HTMLElement;

        expect(element.classList.contains('fd-page-container')).toBe(true);
        expect(element.style.display).toBe('flex');
        expect(element.style.flexDirection).toBe('column');
        expect(element.style.gap).toBe('var(--fd-page-body-gap)');
        expect(element.style.maxWidth).toBe('var(--fd-layout-page-content-max-width)');
    });
});
