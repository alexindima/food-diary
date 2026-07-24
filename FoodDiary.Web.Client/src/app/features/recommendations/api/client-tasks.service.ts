import { Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import type { ClientTask, ClientTaskStatus } from '../../../shared/models/dietologist.data';

@Service()
export class ClientTasksService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.clientTasks;

    public getMyTasks(): Observable<ClientTask[]> {
        return this.get<ClientTask[]>('');
    }

    public changeStatus(taskId: string, status: Extract<ClientTaskStatus, 'Open' | 'Completed'>): Observable<ClientTask> {
        return this.put<ClientTask>(`${taskId}/status`, { status });
    }
}
