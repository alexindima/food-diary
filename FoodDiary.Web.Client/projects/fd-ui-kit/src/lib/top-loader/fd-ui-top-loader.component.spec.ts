import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiTopLoaderComponent } from './fd-ui-top-loader.component';

describe('FdUiTopLoaderComponent', () => {
    let component: FdUiTopLoaderComponent;
    let fixture: ComponentFixture<FdUiTopLoaderComponent>;

    const loader = (): HTMLElement => {
        const host = fixture.nativeElement as HTMLElement;
        const element = host.querySelector<HTMLElement>('.fd-ui-top-loader');
        if (element === null) {
            throw new Error('Expected top loader to exist.');
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiTopLoaderComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiTopLoaderComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should toggle visible modifier class', () => {
        expect(loader().classList.contains('fd-ui-top-loader--visible')).toBe(false);

        fixture.componentRef.setInput('visible', true);
        fixture.detectChanges();

        expect(loader().classList.contains('fd-ui-top-loader--visible')).toBe(true);
    });
});
