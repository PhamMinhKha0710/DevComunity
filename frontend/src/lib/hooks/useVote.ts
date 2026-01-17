'use client';

import { useState, useCallback } from 'react';
import apiClient from '@/lib/api/client';

type VoteType = 'up' | 'down' | 'none';
type ItemType = 'question' | 'answer';

interface VoteState {
    score: number;
    userVote: VoteType;
    isLoading: boolean;
    error: string | null;
}

interface UseVoteReturn extends VoteState {
    vote: (voteType: VoteType) => Promise<void>;
    removeVote: () => Promise<void>;
}

/**
 * React hook for handling votes on questions and answers
 * Converted from public/js/vote-handler.js
 */
export function useVote(
    itemId: number,
    itemType: ItemType,
    initialScore: number = 0,
    initialUserVote: VoteType = 'none'
): UseVoteReturn {
    const [state, setState] = useState<VoteState>({
        score: initialScore,
        userVote: initialUserVote,
        isLoading: false,
        error: null,
    });

    const vote = useCallback(async (voteType: VoteType) => {
        setState(prev => ({ ...prev, isLoading: true, error: null }));

        try {
            // Determine if we're toggling off or setting new vote
            const newVoteType = voteType === state.userVote ? 'none' : voteType;

            const endpoint = itemType === 'question'
                ? `/votes/questions/${itemId}`
                : `/votes/answers/${itemId}`;

            if (newVoteType === 'none') {
                // Remove vote
                await apiClient.delete(endpoint);
            } else {
                // Cast vote
                await apiClient.post(endpoint, { voteType: newVoteType });
            }

            // Calculate new score
            let scoreDelta = 0;
            if (state.userVote === 'up') scoreDelta -= 1;
            else if (state.userVote === 'down') scoreDelta += 1;

            if (newVoteType === 'up') scoreDelta += 1;
            else if (newVoteType === 'down') scoreDelta -= 1;

            setState(prev => ({
                ...prev,
                score: prev.score + scoreDelta,
                userVote: newVoteType,
                isLoading: false,
            }));
        } catch (err: any) {
            setState(prev => ({
                ...prev,
                isLoading: false,
                error: err.response?.data?.message || 'Failed to vote',
            }));
        }
    }, [itemId, itemType, state.userVote]);

    const removeVote = useCallback(async () => {
        await vote('none');
    }, [vote]);

    return {
        ...state,
        vote,
        removeVote,
    };
}

export default useVote;
