import { inject, Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HttpTransportType } from '@microsoft/signalr';
import { ConfigService } from '../config/config.service';
import { KeycloakService } from '../auth/keycloak.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private connection!: HubConnection;
  private config = inject(ConfigService);
  private keycloak = inject(KeycloakService);

  private tokenHandlers: ((requestId: string, sessionId: string, token: string) => void)[] = [];
  private completedHandlers: ((requestId: string, sessionId: string) => void)[] = [];
  private gaveUpHandlers: ((requestId: string, sessionId: string, reason: string) => void)[] = [];

  connect(): void {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${this.config.get().notificationUrl}/hubs/chat`, {
        accessTokenFactory: () => this.keycloak.getValidToken(),
        // Force HTTP-based transports as Kong setup doesn't support WebSockets
        transport: HttpTransportType.LongPolling | HttpTransportType.ServerSentEvents
      })
      .withAutomaticReconnect()
      .build();

    this.connection.onreconnected(() => this.registerHandlers());
    this.registerHandlers();
    this.connection.start().catch(console.error);
  }

  onToken(handler: (requestId: string, sessionId: string, token: string) => void): void {
    this.tokenHandlers.push(handler);
  }

  onCompleted(handler: (requestId: string, sessionId: string) => void): void {
    this.completedHandlers.push(handler);
  }

  onGaveUp(handler: (requestId: string, sessionId: string, reason: string) => void): void {
    this.gaveUpHandlers.push(handler);
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
      this.tokenHandlers.forEach(h => h(requestId, sessionId, token));
    });
    this.connection.on('ReceiveCompleted', (data) => {
      console.log('[SignalR] ReceiveCompleted:', data);
      const requestId = data.requestId ?? data.RequestId;
      const sessionId = data.sessionId ?? data.SessionId;
      this.completedHandlers.forEach(h => h(requestId, sessionId));
    });
    this.connection.on('ReceiveGaveUp', (data) => {
      console.log('[SignalR] ReceiveGaveUp:', data);
      const requestId = data.requestId ?? data.RequestId;
      const sessionId = data.sessionId ?? data.SessionId;
      const reason = data.reason ?? data.Reason;
      this.gaveUpHandlers.forEach(h => h(requestId, sessionId, reason));
    });
  }

  disconnect(): void {
    this.connection?.stop();
  }
}
