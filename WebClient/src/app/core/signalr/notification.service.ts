import { inject, Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HttpTransportType } from '@microsoft/signalr';
import { ConfigService } from '../config/config.service';
import { KeycloakService } from '../auth/keycloak.service';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private connection!: HubConnection;
  private config = inject(ConfigService);
  private keycloak = inject(KeycloakService);

  private token$$ = new Subject<{requestId: string, sessionId: string, token: string}>();
  private completed$$ = new Subject<{requestId: string, sessionId: string}>();
  private gaveUp$$ = new Subject<{requestId: string, sessionId: string, reason: string}>();

  readonly token$ = this.token$$.asObservable();
  readonly completed$ = this.completed$$.asObservable();
  readonly gaveUp$ = this.gaveUp$$.asObservable();

  connect(): void {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${this.config.get().notificationUrl}/hubs/chat`, {
        accessTokenFactory: () => this.keycloak.getValidToken(),
        // Force HTTP-based transports as Kong setup doesn't support WebSockets
        transport: HttpTransportType.LongPolling | HttpTransportType.ServerSentEvents
      })
      .withAutomaticReconnect()
      .build();

    this.registerHandlers();
    this.connection.start().catch(console.error);
  }

  private registerHandlers(): void {
    this.connection.off('ReceiveToken');
    this.connection.off('ReceiveCompleted');
    this.connection.off('ReceiveGaveUp');

    this.connection.on('ReceiveToken', (data) => {
      console.log('[SignalR] ReceiveToken:', data);
      const requestId = data.requestId ?? data.RequestId;
      const sessionId = data.sessionId ?? data.SessionId;
      const token = data.token ?? data.Token;
      this.token$$.next({ requestId, sessionId, token });
    });
    this.connection.on('ReceiveCompleted', (data) => {
      console.log('[SignalR] ReceiveCompleted:', data);
      const requestId = data.requestId ?? data.RequestId;
      const sessionId = data.sessionId ?? data.SessionId;
      this.completed$$.next({ requestId, sessionId });
    });
    this.connection.on('ReceiveGaveUp', (data) => {
      console.log('[SignalR] ReceiveGaveUp:', data);
      const requestId = data.requestId ?? data.RequestId;
      const sessionId = data.sessionId ?? data.SessionId;
      const reason = data.reason ?? data.Reason;
      this.gaveUp$$.next({ requestId, sessionId, reason });
    });
  }

  disconnect(): void {
    this.connection?.stop();
  }
}
