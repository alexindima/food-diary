import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { ProductDetailComponent } from './product-detail.component';

describe('ProductDetailComponent', () => {
    let component: ProductDetailComponent;
    let fixture: ComponentFixture<ProductDetailComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProductDetailComponent],
            providers: [
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        id: '1',
                        name: 'Test product',
                        baseAmount: 100,
                        defaultPortionAmount: 100,
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
                    provide: FdUiDialogRef,
                    useValue: jasmine.createSpyObj('FdUiDialogRef', ['close']),
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
