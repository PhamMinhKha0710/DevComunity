'use client';

import type { ConversationParticipant } from '@/types';

interface TypingIndicatorProps {
    otherParticipant?: ConversationParticipant;
}

export default function TypingIndicator({ otherParticipant }: TypingIndicatorProps) {
    return (
        <div className="flex items-start gap-3 mt-2 animate-fadeIn opacity-60">
            <div className="size-9 rounded-full overflow-hidden shrink-0 mt-1">
                {otherParticipant?.profilePicture ? (
                    <img src={otherParticipant.profilePicture} alt="" className="w-full h-full object-cover" />
                ) : (
                    <div className="w-full h-full bg-gradient-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white text-xs font-semibold">
                        {otherParticipant?.displayName?.charAt(0) || otherParticipant?.username?.charAt(0) || '?'}
                    </div>
                )}
            </div>
            <div className="bg-slate-200 dark:bg-slate-800 px-4 py-3 rounded-2xl rounded-tl-none flex gap-1">
                <span className="size-1.5 rounded-full bg-slate-500"></span>
                <span className="size-1.5 rounded-full bg-slate-500"></span>
                <span className="size-1.5 rounded-full bg-slate-500"></span>
            </div>
        </div>
    );
}
