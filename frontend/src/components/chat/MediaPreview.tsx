'use client';

import React from 'react';

interface MediaPreviewProps {
    file: File;
    preview: string;
    messageType: string;
    onRemove: () => void;
    isUploading: boolean;
    uploadProgress: number;
}

const MediaPreview: React.FC<MediaPreviewProps> = ({ 
    file, 
    preview, 
    messageType, 
    onRemove, 
    isUploading, 
    uploadProgress 
}) => {
    const formatFileSize = (bytes: number) => {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    };

    const getFileIcon = () => {
        switch (messageType) {
            case 'image': return '🖼️';
            case 'video': return '🎬';
            case 'audio': return '🎵';
            default: return '📄';
        }
    };

    return (
        <div className="relative p-3 bg-[var(--bg-tertiary)] rounded-xl border border-[var(--border-color)] animate-slideUp">
            <div className="flex items-center gap-3">
                {/* Preview thumbnail */}
                {messageType === 'image' && preview ? (
                    <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-black/20 flex-shrink-0">
                        <img 
                            src={preview} 
                            alt="Preview" 
                            className="w-full h-full object-cover"
                        />
                    </div>
                ) : messageType === 'video' && preview ? (
                    <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-black flex-shrink-0">
                        <video 
                            src={preview} 
                            className="w-full h-full object-cover"
                        />
                        <div className="absolute inset-0 flex items-center justify-center bg-black/30">
                            <span className="text-white text-2xl">▶️</span>
                        </div>
                    </div>
                ) : (
                    <div className="w-16 h-16 rounded-lg bg-gradient-to-br from-gray-500 to-gray-600 flex items-center justify-center text-2xl flex-shrink-0">
                        {getFileIcon()}
                    </div>
                )}

                {/* File info */}
                <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-[var(--text-primary)] truncate">
                        {file.name}
                    </p>
                    <p className="text-xs text-[var(--text-muted)]">
                        {formatFileSize(file.size)} • {messageType.charAt(0).toUpperCase() + messageType.slice(1)}
                    </p>
                    
                    {/* Upload progress */}
                    {isUploading && (
                        <div className="mt-2">
                            <div className="h-1.5 bg-[var(--bg-secondary)] rounded-full overflow-hidden">
                                <div 
                                    className="h-full bg-gradient-to-r from-purple-500 to-pink-500 transition-all duration-300"
                                    style={{ width: `${uploadProgress}%` }}
                                />
                            </div>
                            <p className="text-xs text-[var(--text-muted)] mt-1">
                                Uploading... {uploadProgress}%
                            </p>
                        </div>
                    )}
                </div>

                {/* Remove button */}
                {!isUploading && (
                    <button
                        onClick={onRemove}
                        className="w-8 h-8 rounded-full hover:bg-[var(--bg-secondary)] flex items-center justify-center text-[var(--text-muted)] hover:text-red-500 transition flex-shrink-0"
                    >
                        <span className="material-symbols-outlined">close</span>
                    </button>
                )}
            </div>
        </div>
    );
};

export default MediaPreview;
