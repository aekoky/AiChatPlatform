export interface Message {
  id: string;
  sessionId: string;
  senderId: string;
  content: string;
  role: number;
  sentAt: string;
  isEdited: boolean;
  version: number;
  sources?: string[];
}
