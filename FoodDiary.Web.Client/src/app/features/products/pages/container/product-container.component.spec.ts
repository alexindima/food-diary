import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { ProductContainerComponent } from './product-container.component';

describe('ProductContainerComponent', () => {
    let component: ProductContainerComponent;
    let fixture: ComponentFixture<ProductContainerComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProductContainerComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ProductContainerComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
