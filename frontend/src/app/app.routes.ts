import { Routes } from '@angular/router';
import { ChatComponent } from './components/chat/chat.component';
import { DirectComponent } from './components/direct/direct.component';

export const routes: Routes = [
  { path: '', redirectTo: '/chat', pathMatch: 'full' },
  { path: 'chat', component: ChatComponent },
  { path: 'direct', component: DirectComponent },
  { path: '**', redirectTo: '/chat' }
];
