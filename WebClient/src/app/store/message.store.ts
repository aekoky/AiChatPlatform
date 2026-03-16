import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, pipe } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { Message } from '../models/message.model';
import { ChatService } from '../core/api/chat.service';

const GAVE_UP_MESSAGES: Record<string, string> = {
  'LLM_ERROR': 'The AI encountered an error. Please try again.',
  'LLM_TIMEOUT': 'The AI took too long to respond. Please try again.',
  'MAX_RETRIES_EXCEEDED': 'The AI failed after multiple attempts. Please try again later.',
  'SESSION_DELETED': 'This session was deleted.'
};

export const MessageStore = signalStore(
  { providedIn: 'root' },
  withState({
    messages: [] as Message[],
    streamingContent: null as string | null,
    isStreaming: false,
    loading: false,
    error: null as string | null
  }),
  withMethods((store, chatService = inject(ChatService)) => ({

    loadMessages: rxMethod<string>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(sessionId => chatService.getMessages(sessionId).pipe(
          tap(messages => patchState(store, { messages, loading: false })),
          catchError(err => {
            patchState(store, { error: err.message, loading: false });
            return EMPTY;
          })
        ))
      )
    ),

    sendMessage: rxMethod<{ sessionId: string; content: string }>(
      pipe(
        tap(({ content }) => {
          patchState(store, {
            messages: [...store.messages(), {
              id: crypto.randomUUID(),
              sessionId: '',
              senderId: '',
              version: 0,
              role: 0,
              content,
              sentAt: new Date().toISOString(),
              isEdited: false
            }],
            isStreaming: true,
            streamingContent: '',
            error: null
          });
        }),
        switchMap(({ sessionId, content }) =>
          chatService.sendMessage(sessionId, content).pipe(
            catchError(err => {
              patchState(store, { isStreaming: false, error: err.message });
              return EMPTY;
            })
          )
        )
      )
    ),

    appendToken(token: string): void {
      patchState(store, {
        streamingContent: (store.streamingContent() ?? '') + token
      });
    },

    finalizeStream(): void {
      const content = store.streamingContent();
      if (!content) return;
      patchState(store, {
        messages: [...store.messages(), {
          id: crypto.randomUUID(),
          sessionId: '',
          senderId: '',
          version:0,
          role: 1,
          content,
          sentAt: new Date().toISOString(),
          isEdited: false
        }],
        streamingContent: null,
        isStreaming: false
      });
    },

    handleGaveUp(reason: string): void {
      patchState(store, {
        streamingContent: null,
        isStreaming: false,
        error: GAVE_UP_MESSAGES[reason] ?? 'An unknown error occurred. Please try again.'
      });
    }
  }))
);
