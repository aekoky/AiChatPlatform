import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SessionStore } from '../../../store/session.store';
import { MessageStore } from '../../../store/message.store';
import { SessionItemComponent } from '../session-item/session-item.component';
import { NewChatDialogComponent } from '../../../shared/new-chat-dialog/new-chat-dialog.component';

@Component({
  selector: 'app-session-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    SessionItemComponent
  ],
  templateUrl: './session-list.component.html',
  styleUrls: ['./session-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SessionListComponent {
  sessionStore = inject(SessionStore);
  messageStore = inject(MessageStore);
  private dialog = inject(MatDialog);

  onSessionSelect(sessionId: string): void {
    this.sessionStore.setActiveSession(sessionId);
  }

  openNewChatDialog(): void {
    const dialogRef = this.dialog.open(NewChatDialogComponent, { width: '360px' });
    dialogRef.afterClosed().subscribe((title: string | undefined) => {
      if (title?.trim()) {
        this.sessionStore.startChat(title.trim());
      }
    });
  }
}
