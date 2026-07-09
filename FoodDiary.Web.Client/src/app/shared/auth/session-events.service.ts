import { Service } from '@angular/core';
import { Subject } from 'rxjs';

@Service()
export class SessionEventsService {
    private readonly authenticatedSubject = new Subject<void>();

    public readonly authenticated$ = this.authenticatedSubject.asObservable();

    public notifyAuthenticated(): void {
        this.authenticatedSubject.next();
    }
}
