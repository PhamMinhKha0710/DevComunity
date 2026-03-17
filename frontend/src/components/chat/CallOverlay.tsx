'use client';

import { useEffect, useRef } from 'react';
import type { CallState, CallType } from '@/lib/webrtc/useWebRTC';
import { authorInitial } from './types';

interface CallOverlayProps {
    callState: CallState;
    callType: CallType;
    peerName: string;
    localStream: MediaStream | null;
    remoteStream: MediaStream | null;
    isMuted: boolean;
    isCameraOff: boolean;
    callDuration: number;
    onAccept: () => void;
    onReject: () => void;
    onEnd: () => void;
    onToggleMute: () => void;
    onToggleCamera: () => void;
}

function formatDuration(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
}

export default function CallOverlay({
    callState, callType, peerName, localStream, remoteStream,
    isMuted, isCameraOff, callDuration,
    onAccept, onReject, onEnd, onToggleMute, onToggleCamera,
}: CallOverlayProps) {
    const localVideoRef = useRef<HTMLVideoElement>(null);
    const remoteVideoRef = useRef<HTMLVideoElement>(null);

    useEffect(() => {
        if (localVideoRef.current && localStream) {
            localVideoRef.current.srcObject = localStream;
        }
    }, [localStream]);

    useEffect(() => {
        if (remoteVideoRef.current && remoteStream) {
            remoteVideoRef.current.srcObject = remoteStream;
        }
    }, [remoteStream, callState]);

    useEffect(() => {
        if (callState !== 'incoming') return;
        if (typeof window === 'undefined') return;
        const ctx = new AudioContext();
        let interval: ReturnType<typeof setInterval>;
        const ring = () => {
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.connect(gain).connect(ctx.destination);
            osc.frequency.value = 440;
            gain.gain.value = 0.3;
            osc.start();
            osc.stop(ctx.currentTime + 0.3);
        };
        ring();
        interval = setInterval(ring, 1500);
        return () => {
            clearInterval(interval);
            ctx.close();
        };
    }, [callState]);

    const isVideo = callType === 'video';
    const initial = authorInitial(peerName);

    return (
        <div className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-900/95">
            {/* Video background */}
            {isVideo && callState === 'connected' && (
                <>
                    <video
                        ref={remoteVideoRef}
                        autoPlay
                        playsInline
                        className="absolute inset-0 w-full h-full object-cover"
                    />
                    <video
                        ref={localVideoRef}
                        autoPlay
                        playsInline
                        muted
                        className="absolute bottom-24 right-6 w-40 h-28 rounded-2xl object-cover border-2 border-white/30 shadow-2xl z-10"
                    />
                </>
            )}

            {/* Audio-only or pre-connection: avatar + info */}
            {(!isVideo || callState !== 'connected') && (
                <div className="flex flex-col items-center z-10">
                    {/* Ringing animation for incoming/outgoing */}
                    <div className={`relative ${callState === 'incoming' || callState === 'outgoing' ? 'animate-pulse' : ''}`}>
                        <div className="size-28 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-4xl font-bold shadow-2xl">
                            {initial}
                        </div>
                        {(callState === 'incoming' || callState === 'outgoing') && (
                            <>
                                <div className="absolute inset-0 rounded-full border-4 border-white/20 animate-ping" />
                                <div className="absolute -inset-3 rounded-full border-2 border-white/10 animate-ping [animation-delay:0.3s]" />
                            </>
                        )}
                    </div>

                    <h2 className="mt-6 text-2xl font-bold text-white">{peerName}</h2>

                    {callState === 'incoming' && (
                        <p className="mt-2 text-blue-300 text-sm font-medium animate-pulse">
                            {isVideo ? 'Cuộc gọi video đến...' : 'Cuộc gọi thoại đến...'}
                        </p>
                    )}
                    {callState === 'outgoing' && (
                        <p className="mt-2 text-slate-400 text-sm font-medium">Đang gọi...</p>
                    )}
                    {callState === 'connecting' && (
                        <p className="mt-2 text-yellow-400 text-sm font-medium">Đang kết nối...</p>
                    )}
                    {callState === 'connected' && (
                        <p className="mt-2 text-emerald-400 text-sm font-medium tabular-nums">
                            {formatDuration(callDuration)}
                        </p>
                    )}

                    {/* Hidden video for audio-only connected calls */}
                    {callState === 'connected' && !isVideo && (
                        <>
                            <audio ref={remoteVideoRef as React.RefObject<HTMLAudioElement>} autoPlay />
                        </>
                    )}
                </div>
            )}

            {/* Connected video: overlay timer */}
            {isVideo && callState === 'connected' && (
                <div className="absolute top-6 left-1/2 -translate-x-1/2 z-10 px-4 py-1.5 bg-black/50 backdrop-blur-sm rounded-full">
                    <p className="text-white text-sm font-medium tabular-nums">{formatDuration(callDuration)}</p>
                </div>
            )}

            {/* Controls */}
            <div className="absolute bottom-10 left-1/2 -translate-x-1/2 z-10 flex items-center gap-5">
                {callState === 'incoming' ? (
                    <>
                        <button
                            onClick={onReject}
                            className="size-16 rounded-full bg-red-500 hover:bg-red-600 text-white flex items-center justify-center shadow-lg shadow-red-500/30 transition-all hover:scale-105"
                            title="Từ chối"
                        >
                            <span className="material-symbols-outlined text-3xl">call_end</span>
                        </button>
                        <button
                            onClick={onAccept}
                            className="size-16 rounded-full bg-emerald-500 hover:bg-emerald-600 text-white flex items-center justify-center shadow-lg shadow-emerald-500/30 transition-all hover:scale-105"
                            title="Chấp nhận"
                        >
                            <span className="material-symbols-outlined text-3xl">call</span>
                        </button>
                    </>
                ) : (
                    <>
                        <button
                            onClick={onToggleMute}
                            className={`size-14 rounded-full flex items-center justify-center transition-all hover:scale-105 ${
                                isMuted
                                    ? 'bg-red-500/90 text-white'
                                    : 'bg-white/20 backdrop-blur-sm text-white hover:bg-white/30'
                            }`}
                            title={isMuted ? 'Bật mic' : 'Tắt mic'}
                        >
                            <span className="material-symbols-outlined text-2xl">
                                {isMuted ? 'mic_off' : 'mic'}
                            </span>
                        </button>

                        {isVideo && (
                            <button
                                onClick={onToggleCamera}
                                className={`size-14 rounded-full flex items-center justify-center transition-all hover:scale-105 ${
                                    isCameraOff
                                        ? 'bg-red-500/90 text-white'
                                        : 'bg-white/20 backdrop-blur-sm text-white hover:bg-white/30'
                                }`}
                                title={isCameraOff ? 'Bật camera' : 'Tắt camera'}
                            >
                                <span className="material-symbols-outlined text-2xl">
                                    {isCameraOff ? 'videocam_off' : 'videocam'}
                                </span>
                            </button>
                        )}

                        <button
                            onClick={onEnd}
                            className="size-16 rounded-full bg-red-500 hover:bg-red-600 text-white flex items-center justify-center shadow-lg shadow-red-500/30 transition-all hover:scale-105"
                            title="Kết thúc cuộc gọi"
                        >
                            <span className="material-symbols-outlined text-3xl">call_end</span>
                        </button>
                    </>
                )}
            </div>
        </div>
    );
}
