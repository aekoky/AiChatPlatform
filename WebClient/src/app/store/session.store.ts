import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, pipe } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { Session } from '../models/session.model';
import { ChatService } from '../core/api/chat.service';

export const SessionStore = signalStore(
  { providedIn: 'root' },
  withState({
    sessions: [] as Session[],
    activeSessionId: null as string | null,
    loading: false,
    error: null as string | null
  }),
  withMethods((store,
    chatService = inject(ChatService)) => ({

    loadSessions: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() => chatService.getConversations().pipe(
          tap(sessions => patchState(store, { sessions, loading: false })),
          catchError(err => {
            patchState(store, { error: err.message, loading: false });
            return EMPTY;
          })
        ))
      )
    ),

    setActiveSession(sessionId: string): void {
      patchState(store, { activeSessionId: sessionId });
    },

    startChat: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { loading: true })),
        switchMap(title => chatService.startChat(title).pipe(
          switchMap(() => chatService.getConversations()),
          tap(sessions => {
            patchState(store, { sessions, loading: false });
            // Find the most recently created session with this title and set it active
            const newSession = [...sessions]
              .filter(s => s.title === title)
              .sort((a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime())[0];

            if (newSession) {
              patchState(store, { activeSessionId: newSession.id });
            }
          }),
          catchError(err => {
            patchState(store, { error: err.message, loading: false });
            return EMPTY;
          })
        ))
      )
    ),

    closeConversation: rxMethod<{ sessionId: string; version: number }>(
      pipe(
        switchMap(({ sessionId, version }) => chatService.closeConversation(sessionId, version).pipe(
          tap(() => patchState(store, {
            sessions: store.sessions().filter(s => s.id !== sessionId),
            activeSessionId: store.activeSessionId() === sessionId
              ? null
              : store.activeSessionId()
          })),
          catchError(err => {
            patchState(store, { error: err.message });
            return EMPTY;
          })
        ))
      )
    )
  }))
);
