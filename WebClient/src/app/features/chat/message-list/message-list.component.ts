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

  constructor() {
    // Scroll to bottom whenever messages or streaming content changes
    effect(() => {
      this.messageStore.messages();
      this.messageStore.streamingContent();
      
      // Delay slightly to allow the DOM to update
      setTimeout(() => this.scrollToBottom(), 0);
    });
  }

  private scrollToBottom(): void {
    this.scrollAnchor?.nativeElement?.scrollIntoView({ behavior: 'smooth', block: 'end' });
  }
}
