import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Session } from '../../models/session.model';
import { Message } from '../../models/message.model';
import { ConfigService } from '../config/config.service';


@Injectable({ providedIn: 'root' })
export class ChatService {
  private http = inject(HttpClient);
  private config = inject(ConfigService);

  private get baseUrl(): string {
    return this.config.get().chatApiUrl;
  }

  getConversations(): Observable<Session[]> {
    return this.http
      .get<Session[]>(`${this.baseUrl}/api/Chat/user/conversations`);
  }

  getConversation(sessionId: string): Observable<Session> {
    return this.http
      .get<Session>(`${this.baseUrl}/api/Chat/conversation/${sessionId}`);
  }

  getMessages(sessionId: string): Observable<Message[]> {
    return this.http
      .get<Message[]>(`${this.baseUrl}/api/Chat/conversation/${sessionId}/messages`);
  }

  startChat(title: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/Chat/start`, { title });
  }

  sendMessage(sessionId: string, content: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/Chat/message`, { sessionId, content });
  }

  closeConversation(sessionId: string, version: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/Chat/close`, { sessionId, version });
  }
}
