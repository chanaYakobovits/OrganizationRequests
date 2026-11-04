import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Subject, catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap, tap } from 'rxjs';
import { getErrorMessage, getErrorStatus } from '../core/http-error';
import {PagedResult,RequestDto,RequestQuery,RequestSortField,} from '../models/request.models';
import { RequestsApiService } from './requests-api.service';
import { finalize } from 'rxjs';
import { RequestStatus } from '../models/request.models';
import { SummaryStore } from './summary.store';
import { HistoryStore } from './history.store';

type FilterFields = Partial<
  Pick<
    RequestQuery,
    'status' | 'priority' | 'organizationName' | 'assignedTo' | 'createdFrom' | 'createdTo'
  >
>;

type LoadResult =
  | { ok: true; data: PagedResult<RequestDto> }
  | { ok: false; error: unknown };

const DEFAULT_QUERY: RequestQuery = {
  page: 1,
  pageSize: 20,
  sortBy: 'CreatedAt',
  sortDir: 'Desc',
};

const CURRENT_USER = 'מטפל 01';

@Injectable({ providedIn: 'root' })
export class RequestsStore {

  private readonly history = inject(HistoryStore);
  private readonly api = inject(RequestsApiService);
  private readonly summary = inject(SummaryStore);
 
  private readonly _query = signal<RequestQuery>({ ...DEFAULT_QUERY });
  readonly query = this._query.asReadonly();

  
  readonly items = signal<RequestDto[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly isEmpty = computed(
    () => !this.loading() && !this.error() && this.items().length === 0,
  );

  private readonly searchInput$ = new Subject<string>();

  readonly updatingId = signal<number | null>(null);
  readonly actionMessage = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  constructor() {
    
    this.searchInput$
      .pipe(
        debounceTime(300),
        map((text) => text.trim()),
        // distinctUntilChanged(),
        filter((s) => s !== (this._query().search ?? '')),
        takeUntilDestroyed(),
      )
      .subscribe((search) => this.patchQuery({ search: search || undefined, page: 1 }));

    
    toObservable(this._query)
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap((q) =>
          this.api.getRequests(q).pipe(
            map((data): LoadResult => ({ ok: true, data })),
            catchError((error) => of<LoadResult>({ ok: false, error })),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((res) => {
        this.loading.set(false);
        if (res.ok) {
          this.items.set(res.data.items);
          this.totalCount.set(res.data.totalCount);
          this.totalPages.set(res.data.totalPages);
        } else {
          this.items.set([]);
          this.totalCount.set(0);
          this.totalPages.set(0);
          this.error.set(getErrorMessage(res.error));
        }
      });
  }

updateStatus(item: RequestDto, newStatus: RequestStatus): void {
  if (this.updatingId() !== null) return;

  this.updatingId.set(item.id);
  this.actionMessage.set(null);

  this.api
    .updateStatus(item.id, { newStatus, version: item.version, changedBy: CURRENT_USER })
    .pipe(finalize(() => this.updatingId.set(null)))
    .subscribe({
      next: (updated) => {
        this.items.update((list) => list.map((r) => (r.id === updated.id ? updated : r)));
        this.summary.load();
        if (this.history.selectedId() === updated.id) this.history.refresh();
        this.actionMessage.set({ type: 'success', text: `פנייה #${updated.id} עודכנה.` });
      },
      error: (err) => {
        const status = getErrorStatus(err);
        if (status === 409 || status === 404) this.reload();

        this.actionMessage.set({
          type: 'error',
          text:
            status === 409
              ? 'הפנייה עודכנה בינתיים על ידי משתמש אחר. הרשימה נטענה מחדש, נסי שוב.'
              : getErrorMessage(err),
        });
      },
    });
}

dismissMessage(): void {
  this.actionMessage.set(null);
}
  setSearch(text: string): void {
    this.searchInput$.next(text);
  }

  setFilters(filters: FilterFields): void {
    this.patchQuery({ ...filters, page: 1 });
  }

  setSort(field: RequestSortField): void {
    const q = this._query();
    const sortDir = q.sortBy === field ? (q.sortDir === 'Asc' ? 'Desc' : 'Asc') : 'Asc';
    this.patchQuery({ sortBy: field, sortDir, page: 1 });
  }

  setPage(page: number): void {
    this.patchQuery({ page });
  }

  setPageSize(pageSize: number): void {
    this.patchQuery({ pageSize, page: 1 });
  }

  clearFilters(): void {
    this._query.set({ ...DEFAULT_QUERY, pageSize: this._query().pageSize });
  }

  reload(): void {
    this._query.update((q) => ({ ...q }));
  }

  private patchQuery(patch: Partial<RequestQuery>): void {
    this._query.update((q) => ({ ...q, ...patch }));
  }
}