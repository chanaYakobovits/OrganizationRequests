import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { getErrorMessage } from '../core/http-error';
import {
  PagedResult,
  RequestDto,
  RequestQuery,
  RequestSortField,
} from '../models/request.models';
import { RequestsApiService } from './requests-api.service';

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

@Injectable({ providedIn: 'root' })
export class RequestsStore {
  private readonly api = inject(RequestsApiService);

  // ===== קלט: מה המשתמשת ביקשה =====
  private readonly _query = signal<RequestQuery>({ ...DEFAULT_QUERY });
  readonly query = this._query.asReadonly();

  // ===== פלט: מה השרת החזיר =====
  readonly items = signal<RequestDto[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly isEmpty = computed(
    () => !this.loading() && !this.error() && this.items().length === 0,
  );

  private readonly searchInput$ = new Subject<string>();

  constructor() {
    // Debounce לחיפוש טקסט
    this.searchInput$
      .pipe(
        debounceTime(300),
        map((text) => text.trim()),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((search) => this.patchQuery({ search: search || undefined, page: 1 }));

    // כל שינוי ב-query = טעינה; switchMap מבטל בקשה קודמת
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

  // ===== פעולות =====
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