import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConsumptionListComponent } from './consumption-list.component';

describe('AuthComponent', () => {
    let component: ConsumptionListComponent;
    let fixture: ComponentFixture<ConsumptionListComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ConsumptionListComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ConsumptionListComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
