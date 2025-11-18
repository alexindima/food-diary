import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ProductAddDialogComponent } from './product-add-dialog.component';

describe('ProductAddDialogComponent', () => {
    let component: ProductAddDialogComponent;
    let fixture: ComponentFixture<ProductAddDialogComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProductAddDialogComponent],
            providers: [
                { provide: MAT_DIALOG_DATA, useValue: null },
                { provide: MatDialogRef, useValue: jasmine.createSpyObj('MatDialogRef', ['close']) },
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
