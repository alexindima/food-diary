import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductListDialogComponent } from './product-list-dialog.component';

describe('ProductListDialogComponent', () => {
    let component: ProductListDialogComponent;
    let fixture: ComponentFixture<ProductListDialogComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProductListDialogComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ProductListDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
