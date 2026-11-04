import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { RequestsStore } from './services/requests.store';
import { RequestsList } from './components/requests-list/requests-list';
import { RequestsFilters } from './components/requests-filters/requests-filters';
import { Pager } from './components/pager/pager';
import { RequestsSummary } from './components/requests-summary/requests-summary';
import { RequestHistory } from './components/request-history/request-history';

@Component({
  imports: [RouterOutlet,RequestsList,RequestsFilters,Pager,RequestsSummary,RequestHistory],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('Frontend');
  private readonly store = inject(RequestsStore);
}
