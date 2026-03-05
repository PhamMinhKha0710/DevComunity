'use client';

import React, { useRef, useState } from 'react';

interface MediaPickerProps {
    onFileSelect: (file: File, preview: string, messageType: string) => void;
    onClose: () => void;
    isOpen: boolean;
}

const MediaPicker: React.FC<MediaPickerProps> = ({ onFileSelect, onClose, isOpen }) => {
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [selectedType, setSelectedType] = useState<string | null>(null);

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        const extension = file.name.split('.').pop()?.toLowerCase() || '';
        let messageType = 'file';
        
        if (['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(extension)) {
            messageType = 'image';
        } else if (['mp4', 'webm', 'mov'].includes(extension)) {
            messageType = 'video';
        } else if (['mp3', 'wav', 'ogg', 'm4a'].includes(extension)) {
            messageType = 'audio';
        }

        // Create preview URL for images and videos
        const preview = messageType === 'image' || messageType === 'video' 
            ? URL.createObjectURL(file) 
            : '';

        onFileSelect(file, preview, messageType);
        onClose();
        
        // Reset input
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const triggerFileSelect = (acceptType: string) => {
        setSelectedType(acceptType);
        if (fileInputRef.current) {
            fileInputRef.current.accept = acceptType;
            fileInputRef.current.click();
        }
    };

    if (!isOpen) return null;

    const options = [
        { 
            type: 'image/*', 
            icon: '📷', 
            label: 'Photo', 
            color: 'from-blue-500 to-cyan-500' 
        },
        { 
            type: 'video/*', 
            icon: '🎥', 
            label: 'Video', 
            color: 'from-purple-500 to-pink-500' 
        },
        { 
            type: 'audio/*', 
            icon: '🎵', 
            label: 'Audio', 
            color: 'from-green-500 to-emerald-500' 
        },
        { 
            type: '.pdf,.doc,.docx,.xls,.xlsx,.txt,.zip,.rar', 
            icon: '📎', 
            label: 'File', 
            color: 'from-blue-500 to-sky-500' 
        },
    ];

    return (
        <>
            {/* Backdrop */}
            <div 
                className="fixed inset-0 z-40" 
                onClick={onClose}
            />
            
            {/* Picker panel */}
            <div className="absolute bottom-full left-0 mb-2 p-2 bg-[var(--bg-secondary)] rounded-xl shadow-xl border border-[var(--border-color)] z-50 animate-slideUp">
                <div className="flex gap-2">
                    {options.map((option) => (
                        <button
                            key={option.type}
                            onClick={() => triggerFileSelect(option.type)}
                            className={`flex flex-col items-center justify-center w-16 h-16 rounded-xl bg-gradient-to-br ${option.color} text-white shadow-lg hover:scale-110 transition-transform duration-200`}
                        >
                            <span className="text-2xl">{option.icon}</span>
                            <span className="text-[10px] mt-1 font-medium">{option.label}</span>
                        </button>
                    ))}
                </div>
                <input
                    ref={fileInputRef}
                    type="file"
                    onChange={handleFileChange}
                    className="hidden"
                />
            </div>
        </>
    );
};

export default MediaPicker;
