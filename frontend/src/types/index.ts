// User types
export interface User {
    userId: number;
    username: string;
    email: string;
    displayName?: string;
    profilePicture?: string;
    reputationPoints: number;
    isEmailVerified: boolean;
}

// Auth types
export interface LoginRequest {
    email: string;
    password: string;
    rememberMe?: boolean;
}

export interface RegisterRequest {
    username: string;
    email: string;
    password: string;
    confirmPassword: string;
    displayName?: string;
}

export interface AuthResponse {
    success: boolean;
    message?: string;
    accessToken?: string;
    refreshToken?: string;
    expiresAt?: string;
    user?: User;
}

// Question types
export interface Question {
    questionId: number;
    title: string;
    body: string;
    bodyExcerpt?: string;
    viewCount: number;
    score: number;
    answerCount: number;
    hasAcceptedAnswer: boolean;
    createdDate: string;
    updatedDate?: string;
    status: string;
    authorId?: number;
    authorUsername?: string;
    authorProfilePicture?: string;
    authorReputation?: number;
    tags: Tag[];
    userVoteType?: 'up' | 'down' | null;
    isSaved: boolean;
}

export interface Tag {
    tagId: number;
    tagName: string;
    description?: string;
    usageCount: number;
}

export interface CreateQuestionRequest {
    title: string;
    body: string;
    tags: string[];
}

// Answer types
export interface Answer {
    answerId: number;
    questionId: number;
    body: string;
    score: number;
    isAccepted: boolean;
    createdDate: string;
    updatedDate?: string;
    authorId?: number;
    authorUsername?: string;
    authorProfilePicture?: string;
    authorReputation?: number;
    parentAnswerId?: number;
    childAnswers: Answer[];
    comments: Comment[];
    userVoteType?: 'up' | 'down' | null;
    isSaved: boolean;
}

export interface Comment {
    commentId: number;
    body: string;
    createdDate: string;
    authorId: number;
    authorUsername: string;
    authorProfilePicture?: string;
}

// Pagination
export interface PaginatedResponse<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasPrevious: boolean;
    hasNext: boolean;
}

// API error
export interface ApiError {
    message: string;
    errors?: Record<string, string[]>;
}

// Repository types
export interface Repository {
    repositoryId: number;
    repositoryName: string;
    description?: string;
    visibility: 'Public' | 'Private';
    defaultBranch: string;
    ownerId: number;
    ownerUsername: string;
    ownerProfilePicture?: string;
    giteaRepoId?: number;
    cloneUrl?: string;
    createdDate: string;
    updatedDate?: string;
    starsCount: number;
    forksCount: number;
    language?: string;
}

export interface RepositoryFile {
    name: string;
    path: string;
    type: 'file' | 'dir';
    size?: number;
    sha?: string;
}

export interface RepositoryCommit {
    sha: string;
    message: string;
    authorName: string;
    authorEmail: string;
    authorAvatar?: string;
    committedDate: string;
    additions?: number;
    deletions?: number;
}

export interface CreateRepositoryRequest {
    name: string;
    description?: string;
    isPrivate: boolean;
}

// Chat types  
export interface Conversation {
    conversationId: number;
    title?: string;
    lastMessage?: string;
    lastActivityAt: string;
    participants: ConversationParticipant[];
    unreadCount: number;
}

export interface ConversationParticipant {
    userId: number;
    username: string;
    profilePicture?: string;
}

export interface ChatMessage {
    messageId: number;
    conversationId: number;
    senderId: number;
    senderName: string;
    senderAvatar?: string;
    content: string;
    sentAt: string;
    isRead: boolean;
}

// SavedItem types
export interface SavedItem {
    savedItemId: number;
    userId: number;
    targetType: 'Question' | 'Answer';
    targetId: number;
    createdDate: string;
    question?: Question;
    answer?: Answer;
}
