'use client';

import React, { createContext, useContext, useEffect, useCallback } from 'react';
import { useAuth } from '@/lib/contexts/AuthContext';
import { useChatStore } from '@/lib/stores/chatStore';
import { useHub } from '@/lib/signalr/useHub';
import apiClient from '@/lib/api/client';

interface ChatContextType {
    totalUnreadChats: number;
    fetchUnreadChats: () => Promise<void>;
}

const ChatContext = createContext<ChatContextType>({
    totalUnreadChats: 0,
    fetchUnreadChats: async () => {},
});

export function ChatProvider({ children }: { children: React.ReactNode }) {
    const { user } = useAuth();
    const { totalUnreadChats, setTotalUnreadChats, incrementUnread } = useChatStore();
    const hub = useHub('chat');

    const fetchUnreadChats = useCallback(async () => {
        if (!user) return;
        try {
            const response = await apiClient.get('/chat/conversations');
            const convs = response.data?.items || response.data || [];
            const unread = (Array.isArray(convs) ? convs : []).reduce(
                (acc: number, c: any) => acc + (c.unreadCount || 0),
                0
            );
            setTotalUnreadChats(unread);
        } catch (error) {
            console.error('Failed to fetch total unread chats:', error);
        }
    }, [user, setTotalUnreadChats]);

    useEffect(() => {
        if (user) {
            fetchUnreadChats();
        } else {
            setTotalUnreadChats(0);
        }
    }, [user, fetchUnreadChats, setTotalUnreadChats]);

    useEffect(() => {
        if (!user) return;

        const handleNewMessage = () => { incrementUnread(); fetchUnreadChats(); };
        const handleMessagesRead = () => fetchUnreadChats();
        const handleReceiveMessage = () => fetchUnreadChats();

        hub.on('NewMessageNotification', handleNewMessage);
        hub.on('MessagesRead', handleMessagesRead);
        hub.on('ReceiveMessage', handleReceiveMessage);

        return () => {
            hub.off('NewMessageNotification', handleNewMessage);
            hub.off('MessagesRead', handleMessagesRead);
            hub.off('ReceiveMessage', handleReceiveMessage);
        };
    }, [user, hub, incrementUnread, fetchUnreadChats]);

    return (
        <ChatContext.Provider value={{ totalUnreadChats, fetchUnreadChats }}>
            {children}
        </ChatContext.Provider>
    );
}

export function useChatContext() {
    return useContext(ChatContext);
}
