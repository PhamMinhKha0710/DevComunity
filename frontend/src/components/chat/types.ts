import type { Conversation, ChatMessage } from '@/types';

export interface RealtimeMessage extends ChatMessage {
    status?: 'sending' | 'sent' | 'delivered' | 'read';
    senderAvatar?: string;
    senderDisplayName?: string;
}

/**
 * Normalise numeric-like fields from strings to numbers where it is safe.
 * IMPORTANT: We deliberately do NOT coerce messageId / ReplyToMessageId here
 * to avoid losing precision for 64‑bit IDs. Those stay as strings.
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function normalizeMessage<T>(msg: T): T {
    const result = { ...(msg as any) };
    if (result.conversationId != null) result.conversationId = Number(result.conversationId);
    if (result.senderId != null) result.senderId = Number(result.senderId);
    if (result.attachmentSize != null) result.attachmentSize = Number(result.attachmentSize);
    return result as T;
}

/**
 * Decode HTML entities (&#xE0; &#225; &lt; etc.) back to Unicode.
 * Needed because old messages were stored with HtmlEncoder.Default.Encode
 * which converts Vietnamese diacritics to numeric HTML entities.
 */
export function decodeHtmlEntities(text: string): string {
    if (typeof document === 'undefined' || !text) return text || '';
    const textarea = document.createElement('textarea');
    textarea.innerHTML = text;
    return textarea.value;
}

/**
 * Format a call-event JSON string into a human-readable preview.
 * Call messages store JSON like {"type":"ended","callType":"audio","duration":5}
 */
export function formatCallPreview(text: string): string {
    try {
        const parsed = JSON.parse(text);
        const labels: Record<string, string> = {
            ended: 'Cuộc gọi đã kết thúc',
            rejected: 'Cuộc gọi đã từ chối',
            missed: 'Cuộc gọi nhỡ',
            cancelled: 'Cuộc gọi đã hủy',
        };
        return labels[parsed.type] ?? 'Cuộc gọi';
    } catch {
        return text;
    }
}

export interface SearchUser {
    userId: number;
    username: string;
    displayName?: string;
    profilePicture?: string;
}

export interface MessageGroup {
    senderId: number;
    messages: RealtimeMessage[];
    showAvatar: boolean;
    showTime: boolean;
    timeLabel: string;
}

export type ConnectionStatus = 'connected' | 'connecting' | 'disconnected';

/** Re-export from central utils */
export { authorInitial } from '@/lib/utils';

export function formatTime(dateString: string | null | undefined): string {
    if (!dateString) return '';
    try {
        const normalized = dateString.endsWith('Z') || dateString.includes('+') ? dateString : dateString + 'Z';
        const date = new Date(normalized);
        if (isNaN(date.getTime())) return '';
        const now = new Date();
        const diff = now.getTime() - date.getTime();
        const diffDays = Math.floor(diff / (1000 * 60 * 60 * 24));

        if (diffDays === 0) return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        if (diffDays === 1) return 'Yesterday';
        if (diffDays < 7) return date.toLocaleDateString([], { weekday: 'short' });
        return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
    } catch {
        return '';
    }
}

export function formatTimeLabel(dateString: string): string {
    const normalized = dateString.endsWith('Z') || dateString.includes('+') ? dateString : dateString + 'Z';
    const date = new Date(normalized);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    if (diffDays === 1) return `Yesterday ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
    if (diffDays < 7) return `${date.toLocaleDateString([], { weekday: 'long' })} ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
    return date.toLocaleDateString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

export function formatFileSize(bytes?: number): string {
    if (!bytes) return '';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

export function getParticipantName(conv: Conversation, currentUserId: number): string {
    if (conv.title) return conv.title;
    const others = conv.participants?.filter(p => p.userId !== currentUserId) || [];
    return others.map(p => p.displayName || p.username).join(', ') || 'New Chat';
}

export function getParticipantAvatar(conv: Conversation, currentUserId: number): string | undefined {
    const other = conv.participants?.find(p => p.userId !== currentUserId);
    return other?.profilePicture;
}

export function isParticipantOnline(conv: Conversation, currentUserId: number, onlineUsers: Set<string>): boolean {
    const other = conv.participants?.find(p => p.userId !== currentUserId);
    return other ? onlineUsers.has(String(other.userId)) : false;
}

export function buildMessageGroups(messages: RealtimeMessage[]): MessageGroup[] {
    const groups: MessageGroup[] = [];
    const TIME_GAP = 5 * 60 * 1000;

    messages.forEach((msg, idx) => {
        const prevMsg = messages[idx - 1];
        const nextMsg = messages[idx + 1];
        const prevTime = prevMsg ? new Date(prevMsg.sentDate).getTime() : 0;
        const currTime = new Date(msg.sentDate).getTime();
        const nextTime = nextMsg ? new Date(nextMsg.sentDate).getTime() : 0;

        const showTimeLabel = !prevMsg || (currTime - prevTime > TIME_GAP);
        const isCallMessage = msg.messageType === 'call';
        const sameSenderAsPrev = !isCallMessage && prevMsg && prevMsg.senderId === msg.senderId && prevMsg.messageType !== 'call' && !showTimeLabel;
        const sameSenderAsNext = !isCallMessage && nextMsg && nextMsg.senderId === msg.senderId && nextMsg.messageType !== 'call' && (nextTime - currTime <= TIME_GAP);
        const showAvatar = !sameSenderAsNext;

        if (isCallMessage || !sameSenderAsPrev || groups.length === 0) {
            groups.push({
                senderId: msg.senderId,
                messages: [msg],
                showAvatar: isCallMessage ? false : showAvatar,
                showTime: showTimeLabel,
                timeLabel: showTimeLabel ? formatTimeLabel(msg.sentDate) : '',
            });
        } else {
            const lastGroup = groups[groups.length - 1];
            lastGroup.messages.push(msg);
            lastGroup.showAvatar = showAvatar;
        }
    });

    return groups;
}
