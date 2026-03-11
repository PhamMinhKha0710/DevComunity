import { create } from 'zustand';

interface PresenceState {
    onlineUsers: Set<string>;
    setOnlineUsers: (users: string[]) => void;
    addOnlineUser: (userId: string) => void;
    removeOnlineUser: (userId: string) => void;
    isOnline: (userId: string) => boolean;
}

export const usePresenceStore = create<PresenceState>((set, get) => ({
    onlineUsers: new Set(),

    setOnlineUsers: (users) => set({ onlineUsers: new Set(users) }),

    addOnlineUser: (userId) => set((state) => {
        const next = new Set(state.onlineUsers);
        next.add(userId);
        return { onlineUsers: next };
    }),

    removeOnlineUser: (userId) => set((state) => {
        const next = new Set(state.onlineUsers);
        next.delete(userId);
        return { onlineUsers: next };
    }),

    isOnline: (userId) => get().onlineUsers.has(userId),
}));
