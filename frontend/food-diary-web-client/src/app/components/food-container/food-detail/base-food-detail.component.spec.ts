import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BaseFoodDetailComponent } from './base-food-detail.component';

describe('BaseFoodDetailComponent', () => {
    let component: BaseFoodDetailComponent<string>;
    let fixture: ComponentFixture<BaseFoodDetailComponent<string>>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [BaseFoodDetailComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(BaseFoodDetailComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
