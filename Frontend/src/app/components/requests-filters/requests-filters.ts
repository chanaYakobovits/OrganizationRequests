import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RequestsStore } from '../../services/requests.store';
import {ALL_PRIORITIES, ALL_STATUSES, RequestPriority, RequestStatus,} from '../../models/request.models';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { RequestsApiService } from '../../services/requests-api.service';

@Component({
  selector: 'app-requests-filters',
  templateUrl: './requests-filters.html',
  styleUrl: './requests-filters.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsFilters {
  protected readonly store = inject(RequestsStore);
  protected readonly statuses = ALL_STATUSES;
  protected readonly priorities = ALL_PRIORITIES;
 private readonly api = inject(RequestsApiService);
protected readonly assignees = toSignal(
  this.api.getAssignees().pipe(catchError(() => of<string[]>([]))),
  { initialValue: [] as string[] },
);

onAssignee(value: string): void {
  this.store.setFilters({ assignedTo: value || undefined });
}
  onStatus(value: string): void {
    this.store.setFilters({ status: (value || undefined) as RequestStatus | undefined });
  }

  onPriority(value: string): void {
    this.store.setFilters({ priority: (value || undefined) as RequestPriority | undefined });
  }

  onCreatedFrom(value: string): void {
  this.store.setFilters({ createdFrom: value || undefined });
}

onCreatedTo(value: string): void {
  this.store.setFilters({ createdTo: value || undefined });
}

}