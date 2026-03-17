import {
  ChangeDetectionStrategy, Component, DestroyRef, inject, NgZone, OnDestroy, OnInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SessionStore } from '../../store/session.store';
import { MessageStore } from '../../store/message.store';
import { NotificationService } from '../../core/signalr/notification.service';
import { KeycloakService } from '../../core/auth/keycloak.service';
import { SessionListComponent } from '../sessions/session-list/session-list.component';
import { MessageListComponent } from './message-list/message-list.component';
import { MessageInputComponent } from './message-input/message-input.component';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    CommonModule,
    MatSidenavModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    SessionListComponent,
    MessageListComponent,
    MessageInputComponent
  ],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChatComponent implements OnInit, OnDestroy {
  sessionStore = inject(SessionStore);
  messageStore = inject(MessageStore);
  private notificationService = inject(NotificationService);
  private keycloak = inject(KeycloakService);
  private ngZone = inject(NgZone);
  private destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.sessionStore.loadSessions();
    this.notificationService.connect();

    this.notificationService.token$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ sessionId, token }: { sessionId: string, token: string }) => {
        this.ngZone.run(() => {
          if (sessionId === this.sessionStore.activeSessionId()) {
            this.messageStore.appendToken(token);
          }
        });
      });

    this.notificationService.completed$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ sessionId }: { sessionId: string }) => {
        this.ngZone.run(() => {
          if (sessionId === this.sessionStore.activeSessionId()) {
            this.messageStore.finalizeStream();
          }
        });
      });

    this.notificationService.gaveUp$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ sessionId, reason }: { sessionId: string, reason: string }) => {
        this.ngZone.run(() => {
          if (sessionId === this.sessionStore.activeSessionId()) {
            this.messageStore.handleGaveUp(reason);
          }
        });
      });
  }

  ngOnDestroy(): void {
    this.notificationService.disconnect();
  }

  onSendMessage(content: string): void {
    const sessionId = this.sessionStore.activeSessionId();
    if (!sessionId) return;
    this.messageStore.sendMessage({ sessionId, content });
  }

  onLogout(): void {
    this.keycloak.logout();
  }

  get username(): string {
    return this.keycloak.username;
  }

  get activeSessionTitle(): string {
    const id = this.sessionStore.activeSessionId();
    return this.sessionStore.sessions().find(s => s.id === id)?.title ?? '';
  }
}
