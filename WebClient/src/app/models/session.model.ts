export interface Session {
  id: string;
  userId: string;
  title: string;
  startedAt: string;
  lastActivityAt: string;
  metadata: SessionMetadata;
}
export interface SessionMetadata {
  sessionVersion: number;
  messageVersion: number;
}
