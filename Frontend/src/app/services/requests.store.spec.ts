import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { RequestsStore } from './requests.store';
import { API_BASE_URL } from '../core/api-config';
import { PagedResult, RequestDto } from '../models/request.models';

const URL = `${API_BASE_URL}/requests`;

function makeRequest(over: Partial<RequestDto> = {}): RequestDto {
  return {
    id: 1,
    title: 'פנייה',
    organizationName: 'ארגון',
    status: 'New',
    priority: 'Medium',
    assignedTo: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    version: 'AAAAAAAAAAA=',
    ...over,
  };
}

function page(items: RequestDto[]): PagedResult<RequestDto> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

describe('RequestsStore', () => {
  let store: RequestsStore;
  let http: HttpTestingController;

  // בקשות רשימה בלבד (לא summary / history)
  const listRequests = () => http.match((r) => r.url === URL && r.method === 'GET');

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(RequestsStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  /** מריץ את טעינת ברירת המחדל ועונה עליה, כדי להתחיל כל בדיקה ממצב נקי */
  function flushInitialLoad(items: RequestDto[] = [makeRequest()]) {
    TestBed.tick();
    const [initial] = listRequests();
    initial.flush(page(items));
  }

  it('debounce: הקלדות מהירות שולחות בקשה אחת עם הטקסט האחרון', () => {
    flushInitialLoad();
    vi.useFakeTimers();

    store.setSearch('א');
    vi.advanceTimersByTime(100);
    store.setSearch('אל');
    vi.advanceTimersByTime(100);
    store.setSearch('אלפא');
    vi.advanceTimersByTime(299);
    TestBed.tick();
    expect(listRequests().length).toBe(0); // עדיין לא עבר זמן ה-debounce

    vi.advanceTimersByTime(1);
    TestBed.tick();
    const calls = listRequests();
    expect(calls.length).toBe(1);
    expect(calls[0].request.params.get('search')).toBe('אלפא');
    expect(calls[0].request.params.get('page')).toBe('1');
    calls[0].flush(page([]));
  });

  it('switchMap: שינוי שאילתה מבטל את הבקשה הקודמת שעדיין בדרך', () => {
    flushInitialLoad();

    store.setPage(2);
    TestBed.tick();
    const [first] = listRequests();

    store.setPage(3);
    TestBed.tick();
    const [second] = listRequests();

    expect(first.cancelled).toBe(true);
    expect(second.cancelled).toBe(false);

    second.flush(page([makeRequest({ id: 3 })]));
    expect(store.items().map((r) => r.id)).toEqual([3]);
  });

  it('409: אחרי Conflict הרשימה נטענת מחדש ומוצגת הודעה מתאימה', () => {
    flushInitialLoad([makeRequest({ id: 7 })]);
    const item = store.items()[0];

    store.updateStatus(item, 'InProgress');
    http
      .expectOne(`${URL}/7/status`)
      .flush({ title: 'Concurrency conflict' }, { status: 409, statusText: 'Conflict' });

    // reload() משנה את ה-signal, ולכן נשלחת בקשת רשימה חדשה
    TestBed.tick();
    const reload = listRequests();
    expect(reload.length).toBe(1);
    reload[0].flush(page([makeRequest({ id: 7, status: 'Waiting' })]));

    expect(store.actionMessage()?.type).toBe('error');
    expect(store.actionMessage()?.text).toContain('עודכנה בינתיים');
    expect(store.updatingId()).toBeNull();
  });

  it('שגיאת שרת: error מתמלא, הרשימה ריקה ולא נשארים במצב loading', () => {
    flushInitialLoad();

    store.reload();
    TestBed.tick();
    const [req] = listRequests();
    req.flush({ detail: 'boom' }, { status: 500, statusText: 'Server Error' });

    expect(store.error()).toBe('boom');
    expect(store.items()).toEqual([]);
    expect(store.loading()).toBe(false);
    expect(store.isEmpty()).toBe(false); // שגיאה אינה "ריק"
  });
});