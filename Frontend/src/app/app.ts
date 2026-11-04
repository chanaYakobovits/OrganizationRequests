import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { RequestsStore } from './services/requests.store';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('Frontend');
  private readonly store = inject(RequestsStore);
}
