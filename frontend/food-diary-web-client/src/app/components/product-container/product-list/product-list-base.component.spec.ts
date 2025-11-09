import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductListBaseComponent } from './product-list-base.component';

describe('ProductListBaseComponent', () => {
    let component: ProductListBaseComponent;
    let fixture: ComponentFixture<ProductListBaseComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProductListBaseComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ProductListBaseComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
