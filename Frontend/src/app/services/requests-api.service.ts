import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import {BulkUpdateResult,BulkUpdateStatusRequest, PagedResult,RequestDto, RequestQuery,
     RequestStatusHistoryDto,RequestSummaryDto, UpdateStatusRequest,} from '../models/request.models';

@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/requests`;

  getRequests(query: RequestQuery): Observable<PagedResult<RequestDto>> {
    return this.http.get<PagedResult<RequestDto>>(this.baseUrl, {
      params: this.toHttpParams(query),
    });
  }

  updateStatus(id: number, body: UpdateStatusRequest): Observable<RequestDto> {
    return this.http.patch<RequestDto>(`${this.baseUrl}/${id}/status`, body);
  }

  getHistory(id: number): Observable<RequestStatusHistoryDto[]> {
    return this.http.get<RequestStatusHistoryDto[]>(`${this.baseUrl}/${id}/history`);
  }

  getSummary(): Observable<RequestSummaryDto> {
    return this.http.get<RequestSummaryDto>(`${this.baseUrl}/summary`);
  }

  bulkUpdateStatus(body: BulkUpdateStatusRequest): Observable<BulkUpdateResult> {
    return this.http.post<BulkUpdateResult>(`${this.baseUrl}/bulk-status`, body);
  }


  private toHttpParams(query: RequestQuery): HttpParams {
    const params: Record<string, string> = {};
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params[key] = String(value);
      }
    }
    return new HttpParams({ fromObject: params });
  }
}