import { Routes } from '@angular/router';
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'orders' },
  { path: 'orders', loadComponent: () => import('./orders').then((m) => m.Orders) },
  {
    path: 'orders/Confirmation/:id',
    loadComponent: () => import('./confirmation').then((m) => m.Confirmation),
  },
  { path: '**', redirectTo: 'orders' },
];
