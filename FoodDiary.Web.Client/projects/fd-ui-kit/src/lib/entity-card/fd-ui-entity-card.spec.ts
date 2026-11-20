import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiActivateDirective } from '../interaction/fd-ui-activate.directive';
import { FdUiEntityCardComponent } from './fd-ui-entity-card';
import { FdUiEntityCardHeaderDirective } from './fd-ui-entity-card-header.directive';

@Component({
    imports: [FdUiEntityCardComponent, FdUiEntityCardHeaderDirective],
    template: `
        <fd-ui-entity-card [title]="title()" [meta]="meta()" [imageUrl]="imageUrl()">
            @if (showHeader()) {
                <ng-template fdUiEntityCardHeader>
                    <button type="button" (click)="headerActivated.set(true)">Manage</button>
                </ng-template>
            }
            <p class="projected-content">Body content</p>
        </fd-ui-entity-card>
    `,
})
class TestHostComponent {
    public readonly title = signal('Test Title');
    public readonly meta = signal('');
    public readonly imageUrl = signal<string | null>(null);
    public readonly showHeader = signal(false);
    public readonly headerActivated = signal(false);
}

describe('FdUiEntityCardComponent', () => {
    let fixture: ComponentFixture<TestHostComponent>;
    let hostEl: HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(TestHostComponent);
        hostEl = fixture.nativeElement as HTMLElement;
        fixture.detectChanges();
    });

    it('should create', () => {
        const entityCard = hostEl.querySelector('fd-ui-entity-card');
        expect(entityCard).toBeTruthy();
    });

    it('should display title', () => {
        const titleEl = hostEl.querySelector('.fd-ui-entity-card__title');
        expect(titleEl).toBeTruthy();
        expect(titleEl?.textContent.trim()).toBe('Test Title');
    });

    it('should display meta when provided', () => {
        expect(hostEl.querySelector('.fd-ui-entity-card__meta')).toBeNull();

        fixture.componentInstance.meta.set('100 kcal');
        fixture.detectChanges();

        const metaEl = hostEl.querySelector('.fd-ui-entity-card__meta');
        expect(metaEl).toBeTruthy();
        expect(metaEl?.textContent.trim()).toBe('100 kcal');
    });

    it('should render image when imageUrl provided', () => {
        fixture = TestBed.createComponent(TestHostComponent);
        hostEl = fixture.nativeElement as HTMLElement;
        fixture.componentInstance.imageUrl.set('https://example.com/food.jpg');
        fixture.detectChanges();

        const img = hostEl.querySelector('.fd-ui-entity-card__media img') as HTMLImageElement;
        expect(img).toBeTruthy();
        expect(img.src).toBe('https://example.com/food.jpg');
    });

    it('should project content', () => {
        const projected = hostEl.querySelector('.projected-content');
        expect(projected).toBeTruthy();
        expect(projected?.textContent.trim()).toBe('Body content');
    });

    it('renders an interactive projected header on its own and removes it when the template is withdrawn', () => {
        const host = fixture.componentInstance;
        host.title.set('');
        host.showHeader.set(true);
        fixture.detectChanges();

        const action = hostEl.querySelector<HTMLButtonElement>('.fd-ui-entity-card__header-extra button');
        expect(action?.textContent.trim()).toBe('Manage');
        action?.click();
        expect(host.headerActivated()).toBe(true);

        host.showHeader.set(false);
        fixture.detectChanges();
        expect(hostEl.querySelector('.fd-ui-entity-card__header')).toBeNull();
        expect(hostEl.querySelector('.projected-content')?.textContent.trim()).toBe('Body content');
    });

    it('recovers from a failed image when its source is replaced', () => {
        fixture.componentInstance.imageUrl.set('/before.jpg');
        fixture.detectChanges();
        hostEl.querySelector('img')?.dispatchEvent(new Event('error'));
        fixture.detectChanges();
        expect(hostEl.querySelector('img')?.getAttribute('src')).toBe('assets/images/vegetables.svg');
        fixture.componentInstance.imageUrl.set('/after.jpg');
        fixture.detectChanges();
        expect(hostEl.querySelector('img')?.getAttribute('src')).toBe('/after.jpg');
    });
});

describe('FdUiEntityCardComponent with host activation', () => {
    it('keeps the custom header interactive when the card is composed with activation', async () => {
        await TestBed.configureTestingModule({ imports: [TestHostComponent] })
            .overrideComponent(FdUiEntityCardComponent, { add: { hostDirectives: [FdUiActivateDirective] } })
            .compileComponents();
        const fixture = TestBed.createComponent(TestHostComponent);
        fixture.componentInstance.showHeader.set(true);
        fixture.detectChanges();

        const card = fixture.debugElement.query(By.directive(FdUiEntityCardComponent));
        const element = card.nativeElement as HTMLElement;
        const activated = vi.fn();
        card.injector.get(FdUiActivateDirective).fdUiActivated.subscribe(activated);
        const headerAction = element.querySelector<HTMLButtonElement>('.fd-ui-entity-card__header-extra button');
        expect(element.getAttribute('tabindex')).toBe('0');
        expect(headerAction?.textContent.trim()).toBe('Manage');
        headerAction?.click();
        expect(fixture.componentInstance.headerActivated()).toBe(true);
        expect(activated).not.toHaveBeenCalled();

        element.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
        expect(activated).toHaveBeenCalledOnce();
    });
});
