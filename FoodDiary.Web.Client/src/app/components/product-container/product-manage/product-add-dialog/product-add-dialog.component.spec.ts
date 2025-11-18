import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { ProductAddDialogComponent } from './product-add-dialog.component';

describe('ProductAddDialogComponent', () => {
    let component: ProductAddDialogComponent;
    let fixture: ComponentFixture<ProductAddDialogComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProductAddDialogComponent],
            providers: [
                { provide: FD_UI_DIALOG_DATA, useValue: null },
                { provide: FdUiDialogRef, useValue: jasmine.createSpyObj('FdUiDialogRef', ['close']) },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ProductAddDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
