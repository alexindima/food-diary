import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { PasswordResetComponent } from './password-reset.component';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';

describe('PasswordResetComponent', () => {
    let component: PasswordResetComponent;
    let fixture: ComponentFixture<PasswordResetComponent>;
    let authServiceSpy: jasmine.SpyObj<AuthService>;
    let navigationServiceSpy: jasmine.SpyObj<NavigationService>;
    let translateServiceSpy: jasmine.SpyObj<TranslateService>;

    function createComponent(queryParams: Record<string, string> = { userId: 'user-1', token: 'tok-abc' }): void {
        authServiceSpy = jasmine.createSpyObj('AuthService', ['confirmPasswordReset']);
        navigationServiceSpy = jasmine.createSpyObj('NavigationService', ['navigateToHome', 'navigateToAuth']);
        navigationServiceSpy.navigateToHome.and.returnValue(Promise.resolve());
        navigationServiceSpy.navigateToAuth.and.returnValue(Promise.resolve());

        TestBed.configureTestingModule({
            imports: [PasswordResetComponent, TranslateModule.forRoot()],
            providers: [
                { provide: AuthService, useValue: authServiceSpy },
                { provide: NavigationService, useValue: navigationServiceSpy },
                {
                    provide: ActivatedRoute,
                    useValue: {
                        snapshot: {
                            queryParamMap: convertToParamMap(queryParams),
                        },
                    },
                },
            ],
        });

        translateServiceSpy = TestBed.inject(TranslateService) as jasmine.SpyObj<TranslateService>;
        spyOn(translateServiceSpy, 'instant').and.callFake(((key: string | string[]) => key as string) as any);

        fixture = TestBed.createComponent(PasswordResetComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize form with password and confirmPassword fields', () => {
        createComponent();
        expect(component.form.contains('password')).toBeTrue();
        expect(component.form.contains('confirmPassword')).toBeTrue();
    });

    it('should validate required password', () => {
        createComponent();
        const control = component.form.controls.password;
        control.setValue('');
        control.markAsTouched();
        expect(control.hasError('required')).toBeTrue();
    });

    it('should validate password minimum length', () => {
        createComponent();
        const control = component.form.controls.password;
        control.setValue('abc');
        control.markAsTouched();
        expect(control.hasError('minlength')).toBeTrue();

        control.setValue('abcdef');
        expect(control.hasError('minlength')).toBeFalse();
    });

    it('should validate password confirmation match', () => {
        createComponent();
        component.form.controls.password.setValue('validPass1');
        component.form.controls.confirmPassword.setValue('differentPass');
        component.form.controls.confirmPassword.markAsTouched();

        expect(component.form.controls.confirmPassword.hasError('matchField')).toBeTrue();

        component.form.controls.confirmPassword.setValue('validPass1');
        expect(component.form.controls.confirmPassword.hasError('matchField')).toBeFalse();
    });

    it('should call confirmPasswordReset on submit', () => {
        createComponent();
        authServiceSpy.confirmPasswordReset.and.returnValue(of({} as any));

        component.form.controls.password.setValue('newPassword123');
        component.form.controls.confirmPassword.setValue('newPassword123');
        component.onSubmit();

        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledTimes(1);
        const arg = authServiceSpy.confirmPasswordReset.calls.mostRecent().args[0];
        expect(arg.userId).toBe('user-1');
        expect(arg.token).toBe('tok-abc');
        expect(arg.newPassword).toBe('newPassword123');
    });

    it('should navigate to home on successful submit', () => {
        createComponent();
        authServiceSpy.confirmPasswordReset.and.returnValue(of({} as any));

        component.form.controls.password.setValue('newPassword123');
        component.form.controls.confirmPassword.setValue('newPassword123');
        component.onSubmit();

        expect(navigationServiceSpy.navigateToHome).toHaveBeenCalled();
        expect(component.isSubmitting()).toBeFalse();
    });

    it('should handle submit error', () => {
        createComponent();
        authServiceSpy.confirmPasswordReset.and.returnValue(throwError(() => new Error('fail')));

        component.form.controls.password.setValue('newPassword123');
        component.form.controls.confirmPassword.setValue('newPassword123');
        component.onSubmit();

        expect(component.state()).toBe('error');
        expect(component.errorMessage()).toBe('AUTH.RESET.ERROR_GENERIC');
        expect(component.isSubmitting()).toBeFalse();
    });

    it('should set invalid state when token is missing', () => {
        createComponent({ userId: '', token: '' });
        expect(component.state()).toBe('invalid');
        expect(component.errorMessage()).toBe('AUTH.RESET.INVALID');
    });

    it('should not submit when form is invalid', () => {
        createComponent();
        component.form.controls.password.setValue('');
        component.onSubmit();
        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });

    it('should not submit when already submitting', () => {
        createComponent();
        authServiceSpy.confirmPasswordReset.and.returnValue(of({} as any));

        component.form.controls.password.setValue('newPassword123');
        component.form.controls.confirmPassword.setValue('newPassword123');
        component.isSubmitting.set(true);
        component.onSubmit();

        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });
});
