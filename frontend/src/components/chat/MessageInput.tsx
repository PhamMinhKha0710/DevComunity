'use client';

import { useState, useRef } from 'react';
import apiClient from '@/lib/api/client';
import MediaPicker from '@/components/chat/MediaPicker';
import MediaPreview from '@/components/chat/MediaPreview';
import type { RealtimeMessage } from './types';

interface MessageInputProps {
    replyingTo: RealtimeMessage | null;
    onCancelReply: () => void;
    onSend: (content: string) => Promise<void>;
    onSendMedia: (upload: { url: string; fileName: string; fileSize: number; messageType: string }, caption: string) => Promise<void>;
    onTyping: () => void;
}

export default function MessageInput({ replyingTo, onCancelReply, onSend, onSendMedia, onTyping }: MessageInputProps) {
    const [text, setText] = useState('');
    const [showMediaPicker, setShowMediaPicker] = useState(false);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [filePreview, setFilePreview] = useState('');
    const [fileMessageType, setFileMessageType] = useState('');
    const [isUploading, setIsUploading] = useState(false);
    const [uploadProgress, setUploadProgress] = useState(0);
    const sendingRef = useRef(false);

    const handleFileSelect = (file: File, preview: string, messageType: string) => {
        setSelectedFile(file);
        setFilePreview(preview);
        setFileMessageType(messageType);
    };

    const clearSelectedFile = () => {
        if (filePreview) URL.revokeObjectURL(filePreview);
        setSelectedFile(null);
        setFilePreview('');
        setFileMessageType('');
    };

    const handleSend = async () => {
        if (sendingRef.current) return;
        const content = text.trim();
        if (!content) return;

        sendingRef.current = true;
        setText('');
        try {
            await onSend(content);
        } catch {
            setText(content);
        } finally {
            sendingRef.current = false;
        }
    };

    const handleSendMedia = async () => {
        if (!selectedFile || sendingRef.current) return;

        sendingRef.current = true;
        setIsUploading(true);
        setUploadProgress(0);

        try {
            const formData = new FormData();
            formData.append('file', selectedFile);
            const uploadResponse = await apiClient.post('/media/upload', formData, {
                headers: { 'Content-Type': 'multipart/form-data' },
                onUploadProgress: (progressEvent) => {
                    const progress = progressEvent.total
                        ? Math.round((progressEvent.loaded * 100) / progressEvent.total)
                        : 0;
                    setUploadProgress(progress);
                },
            });

            const caption = text.trim();
            clearSelectedFile();
            setText('');
            await onSendMedia(uploadResponse.data, caption);
        } catch {
            // File/text state already cleared on success path; on failure the upload itself failed
        } finally {
            setIsUploading(false);
            setUploadProgress(0);
            sendingRef.current = false;
        }
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setText(e.target.value);
        onTyping();
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') {
            selectedFile ? handleSendMedia() : handleSend();
        }
    };

    return (
        <div className="border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
            {replyingTo && (
                <div className="px-4 py-2 bg-slate-50 dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 flex items-center gap-3 animate-slideUp">
                    <div className="w-1 h-10 bg-[var(--primary)] rounded-full"></div>
                    <div className="flex-1 min-w-0">
                        <div className="text-xs font-semibold text-[var(--primary)]">
                            Replying to {replyingTo.senderUsername}
                        </div>
                        <div className="text-sm text-slate-500 truncate">
                            {replyingTo.content}
                        </div>
                    </div>
                    <button
                        onClick={onCancelReply}
                        className="w-8 h-8 rounded-full hover:bg-slate-100 dark:hover:bg-slate-700 flex items-center justify-center text-slate-400 hover:text-slate-600 transition"
                    >
                        <span className="material-symbols-outlined">close</span>
                    </button>
                </div>
            )}

            {selectedFile && (
                <div className="px-4 pt-3">
                    <MediaPreview
                        file={selectedFile}
                        preview={filePreview}
                        messageType={fileMessageType}
                        onRemove={clearSelectedFile}
                        isUploading={isUploading}
                        uploadProgress={uploadProgress}
                    />
                </div>
            )}

            <div className="p-4">
                <div className="flex items-end gap-3 bg-slate-100 dark:bg-slate-800 p-2 rounded-2xl relative">
                    <div className="flex pb-1">
                        <button
                            onClick={() => setShowMediaPicker(!showMediaPicker)}
                            className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500"
                        >
                            <span className="material-symbols-outlined">add_circle</span>
                        </button>
                        <button className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500">
                            <span className="material-symbols-outlined">image</span>
                        </button>
                        <button className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500">
                            <span className="material-symbols-outlined">attach_file</span>
                        </button>
                        <button className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500">
                            <span className="material-symbols-outlined">mic</span>
                        </button>
                    </div>

                    <MediaPicker
                        isOpen={showMediaPicker}
                        onClose={() => setShowMediaPicker(false)}
                        onFileSelect={handleFileSelect}
                    />

                    <div className="flex-1">
                        <input
                            type="text"
                            placeholder={selectedFile ? 'Add a caption...' : (replyingTo ? `Reply to ${replyingTo.senderUsername}...` : 'Type a message...')}
                            value={text}
                            onChange={handleInputChange}
                            onKeyDown={handleKeyDown}
                            className="w-full bg-transparent border-none focus:ring-0 text-sm py-2 px-0 text-slate-900 dark:text-white placeholder-slate-400"
                        />
                    </div>
                    <div className="flex pb-1 gap-1">
                        <button className="flex size-9 items-center justify-center rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-500">
                            <span className="material-symbols-outlined">mood</span>
                        </button>
                        <button
                            onClick={selectedFile ? handleSendMedia : handleSend}
                            disabled={selectedFile ? isUploading : !text.trim()}
                            className="flex size-10 items-center justify-center rounded-xl bg-[var(--primary)] text-white shadow-lg shadow-[var(--primary)]/30 disabled:opacity-50"
                        >
                            {isUploading ? (
                                <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                            ) : (
                                <span className="material-symbols-outlined">send</span>
                            )}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}
