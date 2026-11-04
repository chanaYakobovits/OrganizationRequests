import { Injectable, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { getErrorMessage } from '../core/http-error';
import { RequestSummaryDto } from '../models/request.models';
import { RequestsApiService } from './requests-api.service';

@Injectable({ providedIn: 'root' })
export class SummaryStore {
  private readonly api = inject(RequestsApiService);
  private sub?: Subscription;

  readonly data = signal<RequestSummaryDto | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  load(): void {
    this.sub?.unsubscribe(); 
    this.loading.set(true);
    this.error.set(null);

    this.sub = this.api.getSummary().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: (e) => {
        this.error.set(getErrorMessage(e));
        this.loading.set(false);
      },
    });
  }
}