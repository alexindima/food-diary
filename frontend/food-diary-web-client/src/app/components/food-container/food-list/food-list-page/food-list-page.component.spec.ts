import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FoodListPageComponent } from './food-list-page.component';

describe('AuthComponent', () => {
    let component: FoodListPageComponent;
    let fixture: ComponentFixture<FoodListPageComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FoodListPageComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FoodListPageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
