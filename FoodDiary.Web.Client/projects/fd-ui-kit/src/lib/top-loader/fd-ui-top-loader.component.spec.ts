import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';
import { FdUiTopLoaderComponent } from './fd-ui-top-loader.component';

describe('FdUiTopLoaderComponent', () => {
    let component: FdUiTopLoaderComponent;
    let fixture: ComponentFixture<FdUiTopLoaderComponent>;

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
        const loader = fixture.debugElement.query(By.css('.fd-ui-top-loader'));

        expect(loader.nativeElement.classList.contains('fd-ui-top-loader--visible')).toBe(false);

        fixture.componentRef.setInput('visible', true);
        fixture.detectChanges();

        expect(loader.nativeElement.classList.contains('fd-ui-top-loader--visible')).toBe(true);
    });
});
