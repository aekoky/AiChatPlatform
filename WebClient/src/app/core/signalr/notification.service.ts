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
  private sources$$ = new Subject<{requestId: string, sessionId: string, sources: string[]}>();
  private completed$$ = new Subject<{requestId: string, sessionId: string}>();
  private gaveUp$$ = new Subject<{requestId: string, sessionId: string, reason: string}>();
  private titleUpdated$$ = new Subject<{sessionId: string, title: string}>();
  private summaryUpdated$$ = new Subject<{sessionId: string, summary: string}>();

  readonly token$ = this.token$$.asObservable();
  readonly sources$ = this.sources$$.asObservable();
  readonly completed$ = this.completed$$.asObservable();
  readonly gaveUp$ = this.gaveUp$$.asObservable();
  readonly titleUpdated$ = this.titleUpdated$$.asObservable();
  readonly summaryUpdated$ = this.summaryUpdated$$.asObservable();

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
    this.connection.off('ReceiveSources');
    this.connection.off('ReceiveCompleted');
    this.connection.off('ReceiveGaveUp');
    this.connection.off('ReceiveTitleUpdated');
    this.connection.off('ReceiveSummaryUpdated');

    this.connection.on('ReceiveToken', (data) => {
      console.log('[SignalR] ReceiveToken:', data);
      const requestId = data.requestId ?? data.RequestId;
      const sessionId = data.sessionId ?? data.SessionId;
      const token = data.token ?? data.Token;
      this.token$$.next({ requestId, sessionId, token });
    });
    this.connection.on('ReceiveSources', (data) => {
      console.log('[SignalR] ReceiveSources:', data);
      const requestId = data.requestId ?? data.RequestId;
      const sessionId = data.sessionId ?? data.SessionId;
      const sources = data.sources ?? data.Sources;
      this.sources$$.next({ requestId, sessionId, sources });
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
    this.connection.on('ReceiveTitleUpdated', (data) => {
      console.log('[SignalR] ReceiveTitleUpdated:', data);
      const sessionId = data.sessionId ?? data.SessionId;
      const title = data.title ?? data.Title;
      this.titleUpdated$$.next({ sessionId, title });
    });
    this.connection.on('ReceiveSummaryUpdated', (data) => {
      console.log('[SignalR] ReceiveSummaryUpdated:', data);
      const sessionId = data.sessionId ?? data.SessionId;
      const summary = data.summary ?? data.Summary;
      this.summaryUpdated$$.next({ sessionId, summary });
    });
  }

  disconnect(): void {
    this.connection?.stop();
  }
}
