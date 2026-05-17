import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdLayoutPageDirective } from './page-layout.directive';

@Component({
    imports: [FdLayoutPageDirective],
    template: ' <section fdLayoutPage [fdLayoutPageBackground]="background" [fdLayoutPagePadding]="padding">Content</section> ',
})
class PageLayoutHostComponent {
    public background: string | null = null;
    public padding: string | null = null;
}

describe('FdLayoutPageDirective', () => {
    it('should apply default layout styles', () => {
        const fixture = createFixture();
        const element = getSection(fixture);

        expect(element.classList.contains('fd-layout-page')).toBe(true);
        expect(element.style.background).toBe('var(--fd-bg-page)');
        expect(element.style.padding).toBe('var(--fd-layout-page-vertical-padding) var(--fd-layout-page-horizontal-padding)');
    });

    it('should apply input overrides', () => {
        const fixture = createFixture({
            background: '#ffffff',
            padding: '12px',
        });

        const element = getSection(fixture);

        expect(element.style.background).toBe('rgb(255, 255, 255)');
        expect(element.style.padding).toBe('12px');
    });
});

function createFixture(values?: { background: string | null; padding: string | null }): ComponentFixture<PageLayoutHostComponent> {
    const fixture = TestBed.createComponent(PageLayoutHostComponent);
    if (values !== undefined) {
        fixture.componentInstance.background = values.background;
        fixture.componentInstance.padding = values.padding;
    }
    fixture.detectChanges();
    return fixture;
}

function getSection(fixture: ComponentFixture<PageLayoutHostComponent>): HTMLElement {
    const host = fixture.nativeElement as HTMLElement;
    return host.querySelector('section') as HTMLElement;
}
