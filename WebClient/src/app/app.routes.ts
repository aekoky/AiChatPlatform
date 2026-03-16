import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/chat/chat.component').then(m => m.ChatComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
