import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BaseProductManageComponent } from './base-product-manage.component';

describe('BaseProductManageComponent', () => {
    let component: BaseProductManageComponent;
    let fixture: ComponentFixture<BaseProductManageComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [BaseProductManageComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(BaseProductManageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
