import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { getErrorMessage } from '../core/http-error';
import { RequestStatusHistoryDto } from '../models/request.models';
import { RequestsApiService } from './requests-api.service';

type HistoryResult =
  | { ok: true; data: RequestStatusHistoryDto[] }
  | { ok: false; error: unknown };

@Injectable({ providedIn: 'root' })
export class HistoryStore {
  private readonly api = inject(RequestsApiService);

  
  private readonly target = signal<{ id: number } | null>(null);
  readonly selectedId = computed(() => this.target()?.id ?? null);

  readonly items = signal<RequestStatusHistoryDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    toObservable(this.target)
      .pipe(
        tap((t) => {
          this.loading.set(t !== null);
          this.error.set(null);
        }),
        switchMap((t) =>
          t === null
            ? of<HistoryResult | null>(null)
            : this.api.getHistory(t.id).pipe(
                map((data): HistoryResult => ({ ok: true, data })),
                catchError((error) => of<HistoryResult>({ ok: false, error })),
              ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((res) => {
        this.loading.set(false);
        if (res === null) return;
        if (res.ok) {
          this.items.set(res.data);
        } else {
          this.items.set([]);
          this.error.set(getErrorMessage(res.error));
        }
      });
  }

  open(id: number): void {
    if (this.selectedId() !== id) this.items.set([]); 
    this.target.set({ id });
  }

  refresh(): void {
    const id = this.selectedId();
    if (id !== null) this.target.set({ id });
  }

  close(): void {
    this.items.set([]);
    this.target.set(null);
  }
}