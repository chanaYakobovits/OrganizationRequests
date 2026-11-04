import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RequestsStore } from '../../services/requests.store';
import { HistoryStore } from '../../services/history.store';
import {ALLOWED_TRANSITIONS,RequestDto,RequestSortField,RequestStatus,} from '../../models/request.models';
import { RequestHistory } from '../request-history/request-history';

@Component({
  selector: 'app-requests-list',
  imports: [DatePipe],
  templateUrl: './requests-list.html',
  styleUrl: './requests-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsList {
  protected readonly store = inject(RequestsStore);
  protected readonly history = inject(HistoryStore);

  protected readonly statusLabels: Record<RequestStatus, string> = {
    New: 'חדשה',
    InProgress: 'בטיפול',
    Waiting: 'בהמתנה',
    Completed: 'הושלמה',
  };

  protected arrow(field: RequestSortField): string {
    const q = this.store.query();
    return q.sortBy === field ? (q.sortDir === 'Asc' ? '▲' : '▼') : '';
  }

  protected allowed(r: RequestDto): RequestStatus[] {
    return ALLOWED_TRANSITIONS[r.status];
  }

  protected onChangeStatus(r: RequestDto, value: string): void {
    if (value) this.store.updateStatus(r, value as RequestStatus);
  }
}