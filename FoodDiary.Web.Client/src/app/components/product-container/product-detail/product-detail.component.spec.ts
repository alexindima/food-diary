import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ProductDetailComponent } from './product-detail.component';

describe('ProductDetailComponent', () => {
    let component: ProductDetailComponent;
    let fixture: ComponentFixture<ProductDetailComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProductDetailComponent],
            providers: [
                {
                    provide: MAT_DIALOG_DATA,
                    useValue: {
                        id: '1',
                        name: 'Test product',
                        baseAmount: 100,
                        baseUnit: 'g',
                        caloriesPerBase: 0,
                        proteinsPerBase: 0,
                        fatsPerBase: 0,
                        carbsPerBase: 0,
                        isOwnedByCurrentUser: true,
                        usageCount: 0,
                    },
                },
                {
                    provide: MatDialogRef,
                    useValue: jasmine.createSpyObj('MatDialogRef', ['close']),
                },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ProductDetailComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
