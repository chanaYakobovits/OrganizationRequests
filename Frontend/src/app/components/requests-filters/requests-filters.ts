import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RequestsStore } from '../../services/requests.store';
import {ALL_PRIORITIES, ALL_STATUSES, RequestPriority, RequestStatus,} from '../../models/request.models';

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

  onStatus(value: string): void {
    this.store.setFilters({ status: (value || undefined) as RequestStatus | undefined });
  }

  onPriority(value: string): void {
    this.store.setFilters({ priority: (value || undefined) as RequestPriority | undefined });
  }
}