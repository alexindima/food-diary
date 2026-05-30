import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { UserService } from '../api/user.service';
import type { ChangePasswordRequest, SetPasswordRequest, UpdateUserAppearanceDto, User } from '../models/user.data';

@Injectable({ providedIn: 'root' })
export class UserFacade {
    private readonly userService = inject(UserService);

    public readonly user = this.userService.user;

    public getInfo(): Observable<User | null> {
        return this.userService.getInfo();
    }

    public getInfoSilently(): Observable<User | null> {
        return this.userService.getInfoSilently();
    }

    public updateAppearance(data: UpdateUserAppearanceDto): Observable<User | null> {
        return this.userService.updateAppearance(data);
    }

    public changePassword(request: ChangePasswordRequest): Observable<boolean> {
        return this.userService.changePassword(request);
    }

    public setPassword(request: SetPasswordRequest): Observable<boolean> {
        return this.userService.setPassword(request);
    }

    public acceptAiConsent(): Observable<void> {
        return this.userService.acceptAiConsent();
    }
}
