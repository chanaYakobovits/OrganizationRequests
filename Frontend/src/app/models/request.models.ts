export type RequestStatus = 'New' | 'InProgress' | 'Waiting' | 'Completed';
export type RequestPriority = 'Low' | 'Medium' | 'High';
export type RequestSortField =
  | 'CreatedAt' | 'UpdatedAt' | 'Title' | 'OrganizationName' | 'Priority' | 'Status';
export type SortDirection = 'Asc' | 'Desc';
export type UpdateOutcome = 'Success' | 'NotFound' | 'Conflict' | 'InvalidTransition';

export const ALL_STATUSES: RequestStatus[] = ['New', 'InProgress', 'Waiting', 'Completed'];
export const ALL_PRIORITIES: RequestPriority[] = ['Low', 'Medium', 'High'];

export interface RequestDto {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  version: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface RequestQuery {
  page: number;
  pageSize: number;
  status?: RequestStatus;
  priority?: RequestPriority;
  organizationName?: string;
  assignedTo?: string;
  createdFrom?: string;
  createdTo?: string;
  search?: string;
  sortBy: RequestSortField;
  sortDir: SortDirection;
}

export interface UpdateStatusRequest {
  newStatus: RequestStatus;
  version: string;
  changedBy: string;
}

export interface BulkStatusItem { id: number; version: string; }

export interface BulkUpdateStatusRequest {
  newStatus: RequestStatus;
  changedBy: string;
  items: BulkStatusItem[];
}

export interface BulkItemResult {
  id: number;
  outcome: UpdateOutcome;
  request?: RequestDto | null;
  error?: string | null;
}

export interface BulkUpdateResult {
  total: number;
  succeeded: number;
  failed: number;
  results: BulkItemResult[];
}

export interface RequestStatusHistoryDto {
  id: number;
  requestId: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}

export interface StatusCountDto { status: RequestStatus; count: number; }
export interface PriorityCountDto { priority: RequestPriority; count: number; }
export interface RequestSummaryDto {
  total: number;
  byStatus: StatusCountDto[];
  byPriority: PriorityCountDto[];
}

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export const ALLOWED_TRANSITIONS: Record<RequestStatus, RequestStatus[]> = {
  New: ['InProgress', 'Waiting'],
  InProgress: ['Waiting', 'Completed'],
  Waiting: ['InProgress', 'Completed'],
  Completed: [],
};