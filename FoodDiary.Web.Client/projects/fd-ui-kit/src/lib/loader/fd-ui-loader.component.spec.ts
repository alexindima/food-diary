import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { FdUiLoaderComponent } from './fd-ui-loader.component';

describe('FdUiLoaderComponent', () => {
    let component: FdUiLoaderComponent;
    let fixture: ComponentFixture<FdUiLoaderComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiLoaderComponent],
            providers: [provideNoopAnimations()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiLoaderComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render spinner element', () => {
        const spinner = fixture.debugElement.query(By.css('mat-progress-spinner'));
        expect(spinner).toBeTruthy();

        const wrapper = fixture.debugElement.query(By.css('.fd-ui-loader'));
        expect(wrapper).toBeTruthy();
    });
});
