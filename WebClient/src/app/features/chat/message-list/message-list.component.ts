import {
  ChangeDetectionStrategy, Component, ElementRef, inject, ViewChild, effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MessageStore } from '../../../store/message.store';

@Component({
  selector: 'app-message-list',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './message-list.component.html',
  styleUrls: ['./message-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageListComponent {
  @ViewChild('scrollAnchor') scrollAnchor!: ElementRef<HTMLDivElement>;

  messageStore = inject(MessageStore);

  private prevMessageCount = 0;

  constructor() {
    // Scroll to bottom smartly: either it's a completely new message or user is already near bottom.
    effect(() => {
      const messages = this.messageStore.messages();
      this.messageStore.streamingContent();
      
      const isNewMessage = messages.length !== this.prevMessageCount;
      this.prevMessageCount = messages.length;

      if (isNewMessage || this.isNearBottom()) {
        // Delay slightly to allow the DOM to update
        setTimeout(() => this.scrollToBottom(), 0);
      }
    });
  }

  private isNearBottom(): boolean {
    const list = this.scrollAnchor?.nativeElement?.parentElement;
    if (!list) return true;
    
    // Within 150px of bottom is considered "near bottom"
    const position = list.scrollTop + list.clientHeight;
    return (list.scrollHeight - position) <= 150;
  }

  private scrollToBottom(): void {
    this.scrollAnchor?.nativeElement?.scrollIntoView({ behavior: 'smooth', block: 'end' });
  }
}
