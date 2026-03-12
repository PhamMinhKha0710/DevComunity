'use client';

import { useRef, useState, useCallback, useEffect } from 'react';

export type CallState = 'idle' | 'outgoing' | 'incoming' | 'connecting' | 'connected';
export type CallType = 'audio' | 'video';

interface CallInfo {
    peerId: string;
    peerName: string;
    callType: CallType;
}

interface UseWebRTCReturn {
    callState: CallState;
    callInfo: CallInfo | null;
    localStream: MediaStream | null;
    remoteStream: MediaStream | null;
    isMuted: boolean;
    isCameraOff: boolean;
    callDuration: number;
    setIceCandidateSender: (fn: (peerId: string, candidate: string) => void) => void;
    startCall: (peerId: string, peerName: string, type: CallType) => Promise<void>;
    acceptCall: () => Promise<void>;
    endCall: () => void;
    handleIncomingCall: (peerId: string, peerName: string, type: CallType) => void;
    handleCallAccepted: (sendOffer: (peerId: string, sdp: string) => void) => Promise<void>;
    handleReceiveOffer: (peerId: string, sdp: string, sendAnswer: (peerId: string, sdp: string) => void) => Promise<void>;
    handleReceiveAnswer: (sdp: string) => Promise<void>;
    handleReceiveIceCandidate: (candidate: string) => Promise<void>;
    handleCallRejected: () => void;
    handleCallEnded: () => void;
    toggleMute: () => void;
    toggleCamera: () => void;
}

const ICE_SERVERS: RTCConfiguration = {
    iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { urls: 'stun:stun1.l.google.com:19302' },
    ],
};

export function useWebRTC(): UseWebRTCReturn {
    const [callState, setCallState] = useState<CallState>('idle');
    const [callInfo, setCallInfo] = useState<CallInfo | null>(null);
    const [localStream, setLocalStream] = useState<MediaStream | null>(null);
    const [remoteStream, setRemoteStream] = useState<MediaStream | null>(null);
    const [isMuted, setIsMuted] = useState(false);
    const [isCameraOff, setIsCameraOff] = useState(false);
    const [callDuration, setCallDuration] = useState(0);

    const pcRef = useRef<RTCPeerConnection | null>(null);
    const localStreamRef = useRef<MediaStream | null>(null);
    const iceCandidateQueue = useRef<RTCIceCandidateInit[]>([]);
    const timerRef = useRef<NodeJS.Timeout | null>(null);
    const sendIceCandidateRef = useRef<((peerId: string, candidate: string) => void) | null>(null);

    const cleanup = useCallback(() => {
        if (timerRef.current) {
            clearInterval(timerRef.current);
            timerRef.current = null;
        }
        if (pcRef.current) {
            pcRef.current.close();
            pcRef.current = null;
        }
        if (localStreamRef.current) {
            localStreamRef.current.getTracks().forEach(t => t.stop());
            localStreamRef.current = null;
        }
        iceCandidateQueue.current = [];
        sendIceCandidateRef.current = null;
        setLocalStream(null);
        setRemoteStream(null);
        setCallState('idle');
        setCallInfo(null);
        setIsMuted(false);
        setIsCameraOff(false);
        setCallDuration(0);
    }, []);

    useEffect(() => {
        return () => cleanup();
    }, [cleanup]);

    const getMedia = useCallback(async (type: CallType) => {
        const constraints: MediaStreamConstraints = {
            audio: true,
            video: type === 'video',
        };
        const stream = await navigator.mediaDevices.getUserMedia(constraints);
        localStreamRef.current = stream;
        setLocalStream(stream);
        return stream;
    }, []);

    const createPeerConnection = useCallback((peerId: string) => {
        const pc = new RTCPeerConnection(ICE_SERVERS);
        pcRef.current = pc;

        pc.onicecandidate = (e) => {
            if (e.candidate && sendIceCandidateRef.current) {
                sendIceCandidateRef.current(peerId, JSON.stringify(e.candidate.toJSON()));
            }
        };

        const remote = new MediaStream();
        setRemoteStream(remote);

        pc.ontrack = (e) => {
            e.streams[0]?.getTracks().forEach(track => remote.addTrack(track));
            setRemoteStream(new MediaStream(remote.getTracks()));
        };

        pc.onconnectionstatechange = () => {
            if (pc.connectionState === 'connected') {
                setCallState('connected');
                setCallDuration(0);
                timerRef.current = setInterval(() => {
                    setCallDuration(prev => prev + 1);
                }, 1000);
            } else if (pc.connectionState === 'failed' || pc.connectionState === 'disconnected') {
                cleanup();
            }
        };

        return pc;
    }, [cleanup]);

    const setIceCandidateSender = useCallback((fn: (peerId: string, candidate: string) => void) => {
        sendIceCandidateRef.current = fn;
    }, []);

    const startCall = useCallback(async (
        peerId: string, peerName: string, type: CallType,
    ) => {
        try {
            setCallInfo({ peerId, peerName, callType: type });
            setCallState('outgoing');

            const stream = await getMedia(type);
            const pc = createPeerConnection(peerId);
            stream.getTracks().forEach(t => pc.addTrack(t, stream));
        } catch (err) {
            console.error('[WebRTC] Failed to start call:', err);
            cleanup();
        }
    }, [getMedia, createPeerConnection, cleanup]);

    const handleCallAccepted = useCallback(async (
        sendOffer: (peerId: string, sdp: string) => void,
    ) => {
        const pc = pcRef.current;
        const info = callInfo;
        if (!pc || !info) return;

        try {
            setCallState('connecting');
            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);
            sendOffer(info.peerId, JSON.stringify(offer));
        } catch (err) {
            console.error('[WebRTC] Failed to create offer:', err);
            cleanup();
        }
    }, [callInfo, cleanup]);

    const handleIncomingCall = useCallback((peerId: string, peerName: string, type: CallType) => {
        setCallInfo({ peerId, peerName, callType: type });
        setCallState('incoming');
    }, []);

    const acceptCall = useCallback(async () => {
        if (!callInfo) return;

        try {
            setCallState('connecting');
            await getMedia(callInfo.callType);
        } catch (err) {
            console.error('[WebRTC] Failed to get media for accept:', err);
            cleanup();
        }
    }, [callInfo, getMedia, cleanup]);

    const handleReceiveOffer = useCallback(async (
        peerId: string, sdp: string,
        sendAnswer: (peerId: string, sdp: string) => void,
    ) => {
        try {
            const pc = createPeerConnection(peerId);
            const stream = localStreamRef.current;
            if (stream) {
                stream.getTracks().forEach(t => pc.addTrack(t, stream));
            }

            const offer = JSON.parse(sdp);
            await pc.setRemoteDescription(new RTCSessionDescription(offer));

            for (const candidate of iceCandidateQueue.current) {
                await pc.addIceCandidate(new RTCIceCandidate(candidate));
            }
            iceCandidateQueue.current = [];

            const answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            sendAnswer(peerId, JSON.stringify(answer));
        } catch (err) {
            console.error('[WebRTC] Failed to handle offer:', err);
            cleanup();
        }
    }, [createPeerConnection, cleanup]);

    const handleReceiveAnswer = useCallback(async (sdp: string) => {
        try {
            const pc = pcRef.current;
            if (!pc) return;
            const answer = JSON.parse(sdp);
            await pc.setRemoteDescription(new RTCSessionDescription(answer));

            for (const candidate of iceCandidateQueue.current) {
                await pc.addIceCandidate(new RTCIceCandidate(candidate));
            }
            iceCandidateQueue.current = [];
        } catch (err) {
            console.error('[WebRTC] Failed to handle answer:', err);
        }
    }, []);

    const handleReceiveIceCandidate = useCallback(async (candidate: string) => {
        try {
            const parsed: RTCIceCandidateInit = JSON.parse(candidate);
            const pc = pcRef.current;
            if (pc && pc.remoteDescription) {
                await pc.addIceCandidate(new RTCIceCandidate(parsed));
            } else {
                iceCandidateQueue.current.push(parsed);
            }
        } catch (err) {
            console.error('[WebRTC] Failed to handle ICE candidate:', err);
        }
    }, []);

    const handleCallRejected = useCallback(() => {
        cleanup();
    }, [cleanup]);

    const handleCallEnded = useCallback(() => {
        cleanup();
    }, [cleanup]);

    const endCall = useCallback(() => {
        cleanup();
    }, [cleanup]);

    const toggleMute = useCallback(() => {
        const stream = localStreamRef.current;
        if (!stream) return;
        stream.getAudioTracks().forEach(t => { t.enabled = !t.enabled; });
        setIsMuted(prev => !prev);
    }, []);

    const toggleCamera = useCallback(() => {
        const stream = localStreamRef.current;
        if (!stream) return;
        stream.getVideoTracks().forEach(t => { t.enabled = !t.enabled; });
        setIsCameraOff(prev => !prev);
    }, []);

    return {
        callState, callInfo, localStream, remoteStream,
        isMuted, isCameraOff, callDuration,
        setIceCandidateSender,
        startCall, acceptCall, endCall,
        handleIncomingCall, handleCallAccepted,
        handleReceiveOffer, handleReceiveAnswer, handleReceiveIceCandidate,
        handleCallRejected, handleCallEnded,
        toggleMute, toggleCamera,
    };
}
