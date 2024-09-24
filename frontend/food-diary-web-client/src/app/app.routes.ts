import { Routes } from '@angular/router';
import { AuthComponent } from './components/auth/auth.component';
import { MainComponent } from './components/main/main.component';

export const routes: Routes = [
    { path: '', component: MainComponent },
    { path: 'auth/:mode', component: AuthComponent },
    //{ path: 'profile', component: ProfileComponent }
];
