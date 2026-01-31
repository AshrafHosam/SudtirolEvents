import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ChatMessage, RecommendationResponse } from '../../models/api.models';
import { LineBreaksPipe } from '../../pipes/line-breaks.pipe';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, LineBreaksPipe],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  
  messages: ChatMessage[] = [];
  userInput: string = '';
  isLoading: boolean = false;
  error: string | null = null;
  llmAvailable: boolean = false;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.checkStatus();
    this.addWelcomeMessage();
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  checkStatus(): void {
    this.apiService.getChatStatus().subscribe({
      next: (status) => {
        this.llmAvailable = status.llmAvailable;
      },
      error: () => {
        this.llmAvailable = false;
      }
    });
  }

  addWelcomeMessage(): void {
    this.messages.push({
      content: `Hello! 👋 I'm your South Tyrol Weather & Activity Assistant. 

Ask me about weather conditions and activity recommendations for locations in South Tyrol. For example:
• "What's the weather like in Bolzano today?"
• "What can I do in Merano tomorrow?"
• "Any indoor activities in Bressanone?"

How can I help you today?`,
      isUser: false,
      timestamp: new Date()
    });
  }

  sendMessage(): void {
    if (!this.userInput.trim() || this.isLoading) return;

    const userMessage = this.userInput.trim();
    this.userInput = '';
    this.error = null;

    // Add user message
    this.messages.push({
      content: userMessage,
      isUser: true,
      timestamp: new Date()
    });

    this.isLoading = true;

    this.apiService.sendChatMessage(userMessage).subscribe({
      next: (response) => {
        this.messages.push({
          content: response.response,
          isUser: false,
          timestamp: new Date(),
          data: response.data || undefined
        });
        this.isLoading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to get response. Please try again.';
        this.messages.push({
          content: `Sorry, I encountered an error: ${this.error}. Please try again.`,
          isUser: false,
          timestamp: new Date()
        });
        this.isLoading = false;
      }
    });
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private scrollToBottom(): void {
    try {
      if (this.messagesContainer) {
        this.messagesContainer.nativeElement.scrollTop = 
          this.messagesContainer.nativeElement.scrollHeight;
      }
    } catch (err) {}
  }

  getClassificationBadgeClass(classification: string): string {
    switch (classification) {
      case 'Good': return 'badge-good';
      case 'Bad': return 'badge-bad';
      case 'Rainy': return 'badge-rainy';
      case 'Cold': return 'badge-cold';
      case 'Hot': return 'badge-hot';
      case 'Windy': return 'badge-windy';
      default: return '';
    }
  }
}
