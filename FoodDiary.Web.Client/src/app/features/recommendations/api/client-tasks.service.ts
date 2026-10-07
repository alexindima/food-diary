import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { ClientTasksSdk } from '../../../shared/api/sdk/generated/api/client-tasks.service';
import { clientTaskFromSdk } from '../../../shared/api/sdk/recommendation-sdk.mapper';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import type { ClientTask, ClientTaskStatus } from '../../../shared/models/dietologist.data';

@Service()
export class ClientTasksService {
    protected readonly baseUrl = environment.apiUrls.clientTasks;
    private readonly sdk = createSdkConnection(ClientTasksSdk, this.baseUrl, inject(HttpClient));

    public getMyTasks(): Observable<ClientTask[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client.getClientTasks({ version: this.sdk.version, page, limit }).pipe(map(values => values.map(clientTaskFromSdk))),
        );
    }

    public changeStatus(taskId: string, status: Extract<ClientTaskStatus, 'Open' | 'Completed'>): Observable<ClientTask> {
        return this.sdk.client
            .putClientTasksByTaskIdStatus({ version: this.sdk.version, taskId, changeClientTaskStatusHttpRequest: { status } })
            .pipe(map(clientTaskFromSdk));
    }
}
