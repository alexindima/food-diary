import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiLoaderComponent } from './fd-ui-loader.component';

describe('FdUiLoaderComponent', () => {
    let component: FdUiLoaderComponent;
    let fixture: ComponentFixture<FdUiLoaderComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiLoaderComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiLoaderComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render spinner element', () => {
        const spinner = fixture.debugElement.query(By.css('.fd-ui-loader__spinner'));
        expect(spinner).toBeTruthy();

        const wrapper = fixture.debugElement.query(By.css('.fd-ui-loader'));
        expect(wrapper).toBeTruthy();
    });
});
