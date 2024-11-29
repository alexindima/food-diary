import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FoodListBaseComponent } from './food-list-base.component';

describe('AuthComponent', () => {
    let component: FoodListBaseComponent;
    let fixture: ComponentFixture<FoodListBaseComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FoodListBaseComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FoodListBaseComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
