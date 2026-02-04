'use client';

import React, { useEffect } from 'react';

interface MediaLightboxProps {
    isOpen: boolean;
    onClose: () => void;
    mediaUrl: string;
    mediaType: 'image' | 'video';
    fileName?: string;
}

const MediaLightbox: React.FC<MediaLightboxProps> = ({ 
    isOpen, 
    onClose, 
    mediaUrl, 
    mediaType, 
    fileName 
}) => {
    // Close on escape key
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape') onClose();
        };
        
        if (isOpen) {
            window.addEventListener('keydown', handleKeyDown);
            document.body.style.overflow = 'hidden';
        }
        
        return () => {
            window.removeEventListener('keydown', handleKeyDown);
            document.body.style.overflow = 'unset';
        };
    }, [isOpen, onClose]);

    if (!isOpen) return null;

    return (
        <div 
            className="fixed inset-0 z-[100] flex items-center justify-center bg-black/90 backdrop-blur-sm animate-fadeIn"
            onClick={onClose}
        >
            {/* Close button */}
            <button
                onClick={onClose}
                className="absolute top-4 right-4 w-12 h-12 rounded-full bg-white/10 hover:bg-white/20 text-white flex items-center justify-center text-2xl transition z-10"
            >
                <i className="bi bi-x-lg"></i>
            </button>

            {/* Download button */}
            <a
                href={mediaUrl}
                download={fileName}
                target="_blank"
                rel="noopener noreferrer"
                onClick={(e) => e.stopPropagation()}
                className="absolute top-4 right-20 w-12 h-12 rounded-full bg-white/10 hover:bg-white/20 text-white flex items-center justify-center text-xl transition z-10"
            >
                <i className="bi bi-download"></i>
            </a>

            {/* Media content */}
            <div 
                className="max-w-[90vw] max-h-[90vh] animate-scaleIn"
                onClick={(e) => e.stopPropagation()}
            >
                {mediaType === 'image' ? (
                    <img
                        src={mediaUrl}
                        alt={fileName || 'Image'}
                        className="max-w-full max-h-[90vh] object-contain rounded-lg shadow-2xl"
                    />
                ) : (
                    <video
                        src={mediaUrl}
                        controls
                        autoPlay
                        className="max-w-full max-h-[90vh] rounded-lg shadow-2xl"
                    >
                        Your browser does not support the video tag.
                    </video>
                )}
            </div>

            {/* File name */}
            {fileName && (
                <div className="absolute bottom-4 left-1/2 -translate-x-1/2 px-4 py-2 bg-white/10 rounded-lg text-white text-sm">
                    {fileName}
                </div>
            )}
        </div>
    );
};

export default MediaLightbox;
