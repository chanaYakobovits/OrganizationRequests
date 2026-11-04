import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SummaryStore } from '../../services/summary.store';
import { RequestPriority, RequestStatus } from '../../models/request.models';

@Component({
  selector: 'app-requests-summary',
  templateUrl: './requests-summary.html',
  styleUrl: './requests-summary.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsSummary {
  protected readonly store = inject(SummaryStore);

  protected readonly statusLabels: Record<RequestStatus, string> = {
    New: 'חדשה',
    InProgress: 'בטיפול',
    Waiting: 'בהמתנה',
    Completed: 'הושלמה',
  };

  protected readonly priorityLabels: Record<RequestPriority, string> = {
    Low: 'נמוכה',
    Medium: 'בינונית',
    High: 'גבוהה',
  };

  constructor() {
    this.store.load();
  }

  protected percent(count: number, total: number): number {
    return total === 0 ? 0 : Math.round((count / total) * 100);
  }
}