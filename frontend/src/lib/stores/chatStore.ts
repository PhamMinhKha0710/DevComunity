import { create } from 'zustand';

interface ChatState {
    totalUnreadChats: number;
    setTotalUnreadChats: (count: number) => void;
    incrementUnread: () => void;
}

export const useChatStore = create<ChatState>((set) => ({
    totalUnreadChats: 0,

    setTotalUnreadChats: (count) => set({ totalUnreadChats: count }),

    incrementUnread: () => set((state) => ({
        totalUnreadChats: state.totalUnreadChats + 1,
    })),
}));
