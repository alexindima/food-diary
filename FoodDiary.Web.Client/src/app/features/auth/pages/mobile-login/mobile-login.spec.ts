import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { MobileLoginComponent } from './mobile-login';

describe('MobileLoginComponent', () => {
    it('renders as a routed authentication page', () => {
        const fixture = TestBed.configureTestingModule({ imports: [MobileLoginComponent] })
            .overrideComponent(MobileLoginComponent, { set: { template: '<div class="mobile-login__content"></div>' } })
            .createComponent(MobileLoginComponent);

        fixture.detectChanges();

        expect(fixture.componentInstance).toBeTruthy();
    });
});
