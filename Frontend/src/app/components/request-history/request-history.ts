import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { HistoryStore } from '../../services/history.store';
import { RequestStatus } from '../../models/request.models';

@Component({
  selector: 'app-request-history',
  imports: [DatePipe],
  templateUrl: './request-history.html',
  styleUrl: './request-history.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestHistory {
  protected readonly store = inject(HistoryStore);

  protected readonly statusLabels: Record<RequestStatus, string> = {
    New: 'חדשה',
    InProgress: 'בטיפול',
    Waiting: 'בהמתנה',
    Completed: 'הושלמה',
  };
}