import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RequestsStore } from '../../services/requests.store';

@Component({
  selector: 'app-pager',
  templateUrl: './pager.html',
  styleUrl: './pager.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pager {
  protected readonly store = inject(RequestsStore);
  protected readonly sizes = [10, 20, 50, 100];

  onSize(value: string): void {
    this.store.setPageSize(Number(value));
  }
}