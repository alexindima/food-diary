import { Service } from '@angular/core';
import { Subject } from 'rxjs';

@Service()
export class SessionEventsService {
    private readonly authenticatedSubject = new Subject<void>();
    private readonly sessionEndedSubject = new Subject<void>();

    public readonly authenticated$ = this.authenticatedSubject.asObservable();
    public readonly sessionEnded$ = this.sessionEndedSubject.asObservable();

    public notifyAuthenticated(): void {
        this.authenticatedSubject.next();
    }

    public notifySessionEnded(): void {
        this.sessionEndedSubject.next();
    }
}
