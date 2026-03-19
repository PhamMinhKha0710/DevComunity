'use client';

import { useState } from 'react';

interface ReactionPickerProps {
    onReact: (reactionType: string) => void;
    onClose: () => void;
    currentReaction?: string;
}

// Reaction types matching backend
const REACTIONS = [
    { type: 'like', emoji: '👍', label: 'Like' },
    { type: 'love', emoji: '❤️', label: 'Love' },
    { type: 'haha', emoji: '😂', label: 'Haha' },
    { type: 'wow', emoji: '😮', label: 'Wow' },
    { type: 'sad', emoji: '😢', label: 'Sad' },
    { type: 'angry', emoji: '😠', label: 'Angry' },
];

export default function ReactionPicker({ onReact, onClose, currentReaction }: ReactionPickerProps) {
    const [hoveredReaction, setHoveredReaction] = useState<string | null>(null);

    return (
        <div 
            className="flex bg-white dark:bg-slate-800 rounded-full shadow-lg border border-slate-200 dark:border-slate-700 p-1 flex gap-1 animate-reaction-pop z-50"
            onMouseLeave={onClose}
        >
            {REACTIONS.map((reaction) => (
                <button
                    key={reaction.type}
                    onClick={() => onReact(reaction.type)}
                    onMouseEnter={() => setHoveredReaction(reaction.type)}
                    onMouseLeave={() => setHoveredReaction(null)}
                    className={`relative w-9 h-9 flex items-center justify-center rounded-full transition-all duration-200 hover:bg-[var(--bg-tertiary)] ${
                        currentReaction === reaction.type 
                            ? 'bg-[var(--primary)]/20 ring-2 ring-[var(--primary)]' 
                            : ''
                    } ${
                        hoveredReaction === reaction.type ? 'scale-125 -translate-y-2' : ''
                    }`}
                    title={reaction.label}
                >
                    <span className="text-xl">{reaction.emoji}</span>
                    
                    {/* Tooltip */}
                    {hoveredReaction === reaction.type && (
                        <span className="absolute -top-8 left-1/2 -translate-x-1/2 px-2 py-1 bg-black/80 text-white text-xs rounded whitespace-nowrap animate-fadeIn">
                            {reaction.label}
                        </span>
                    )}
                </button>
            ))}
        </div>
    );
}

// Export reactions for use elsewhere
export { REACTIONS };

// Helper to get emoji by type
export function getReactionEmoji(type: string): string {
    const reaction = REACTIONS.find(r => r.type === type);
    return reaction?.emoji || '👍';
}
