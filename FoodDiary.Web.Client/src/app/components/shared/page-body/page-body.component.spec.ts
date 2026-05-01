import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { PageBodyComponent } from './page-body.component';

@Component({
    standalone: true,
    imports: [PageBodyComponent],
    template: '<fd-page-body><p class="projected">Projected content</p></fd-page-body>',
})
class TestHostComponent {}

describe('PageBodyComponent', () => {
    it('should create', async () => {
        await TestBed.configureTestingModule({
            imports: [PageBodyComponent],
        }).compileComponents();

        const fixture = TestBed.createComponent(PageBodyComponent);
        fixture.detectChanges();
        expect(fixture.componentInstance).toBeTruthy();
    });

    it('should have fd-page-body wrapper element', async () => {
        await TestBed.configureTestingModule({
            imports: [PageBodyComponent],
        }).compileComponents();

        const fixture = TestBed.createComponent(PageBodyComponent);
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        expect(el.querySelector('.fd-page-body')).toBeTruthy();
    });

    it('should project content', async () => {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
        }).compileComponents();

        const hostFixture = TestBed.createComponent(TestHostComponent);
        hostFixture.detectChanges();
        const el: HTMLElement = hostFixture.nativeElement;
        const projected = el.querySelector('.projected');
        expect(projected).toBeTruthy();
        expect(projected?.textContent?.trim()).toBe('Projected content');
    });
});
