// MOCK: no backend endpoint. Frontend-owned contract (spec 3.7).

export interface Message {
  readonly id: string;
  readonly organizationId: string;
  readonly senderId: string;
  readonly senderName: string;
  readonly recipientId: string;
  readonly recipientName: string;
  readonly subject: string;
  readonly body: string;
  readonly sentAt: string;
  readonly read: boolean;
}

export interface SendMessageCommand {
  readonly organizationId: string;
  readonly recipientId: string;
  readonly subject: string;
  readonly body: string;
}
