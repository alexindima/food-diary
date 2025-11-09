import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductListPageComponent } from './product-list-page.component';

describe('ProductListPageComponent', () => {
    let component: ProductListPageComponent;
    let fixture: ComponentFixture<ProductListPageComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProductListPageComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ProductListPageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
