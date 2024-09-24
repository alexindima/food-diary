import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TuiBlock, TuiCheckbox, TuiTab, TuiTabsHorizontal } from '@taiga-ui/kit';
import {
    FormBuilder,
    FormGroup,
    FormsModule,
    ReactiveFormsModule,
    Validators
} from '@angular/forms';
import { NgIf } from '@angular/common';
import {
    TuiButton, TuiLabel, TuiTextfieldComponent, TuiTextfieldDirective,
} from '@taiga-ui/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-auth',
    standalone: true,
    imports: [
        ReactiveFormsModule,
        TuiTab,
        FormsModule,
        TuiTabsHorizontal,
        NgIf,
        TuiButton,
        TuiTextfieldComponent,
        TuiLabel,
        TuiTextfieldDirective,
        TuiBlock,
        TuiCheckbox,
        TranslateModule
    ],
    templateUrl: './auth.component.html',
    styleUrl: './auth.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthComponent {
    public activeItemIndex = 0;

    public loginForm: FormGroup;
    public registerForm: FormGroup;

    public constructor(
        private readonly route: ActivatedRoute,
        private readonly fb: FormBuilder,
        private readonly router: Router,
        private readonly authService: AuthService
    ) {
        const mode = this.route.snapshot.params['mode'];
        if (mode === 'login') {
            this.activeItemIndex = 0;
        }
        if (mode === 'register') {
            this.activeItemIndex = 1;
        }

        this.loginForm = this.fb.group({
            email: ['', [Validators.required, Validators.email]],
            password: ['', Validators.required],
            rememberMe: [false]
        });

        this.registerForm = this.fb.group({
            email: ['', [Validators.required, Validators.email]],
            password: ['', Validators.required],
            confirmPassword: ['', Validators.required],
            agreeTerms: [false, Validators.requiredTrue]
        });
    }

    public onTabChange(index: number): void {
        const mode = index === 0 ? 'login' : 'register';
        this.router.navigate(['/auth', mode]);
    }

    public onLoginSubmit(): void {
        if (this.loginForm.valid) {
            this.authService.login(this.loginForm.value).subscribe({
                next: (response) => {
                    console.log('Login successful:', response);
                },
                error: (error) => {
                    console.error('Login failed:', error);
                }
            });
        }
    }

    public onRegisterSubmit(): void {
        if (this.registerForm.valid) {
            this.authService.register(this.registerForm.value).subscribe({
                next: (response) => {
                    console.log('Registration successful:', response);
                },
                error: (error) => {
                    console.error('Registration failed:', error);
                }
            });
        }
    }
}
