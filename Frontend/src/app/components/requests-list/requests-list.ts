import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RequestsStore } from '../../services/requests.store';
import {RequestSortField} from '../../models/request.models'

@Component({
  selector: 'app-requests-list',
  imports: [DatePipe],
  templateUrl: './requests-list.html',
  styleUrl: './requests-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsList {
  protected readonly store = inject(RequestsStore);
  protected arrow(field: RequestSortField): string {
  const q = this.store.query();
  return q.sortBy === field ? (q.sortDir === 'Asc' ? '▲' : '▼') : '';
}
}