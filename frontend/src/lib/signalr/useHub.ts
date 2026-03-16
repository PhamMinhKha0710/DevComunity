'use client';

import { useEffect, useRef, useState, useMemo } from 'react';
import * as signalR from '@microsoft/signalr';
import { signalRManager, type HubName } from './connectionManager';
import { useAuth } from '@/lib/contexts/AuthContext';

export interface SignalRConnection {
    connection: signalR.HubConnection | null;
    connectionState: signalR.HubConnectionState;
    error: Error | null;
}

export function useHub(hubName: HubName) {
    const { user } = useAuth();
    const [connectionState, setConnectionState] = useState<signalR.HubConnectionState>(
        () => signalRManager.getState(hubName)
    );
    const connectedRef = useRef(false);

    useEffect(() => {
        const handleStateChange = (name: HubName, state: signalR.HubConnectionState) => {
            if (name === hubName) setConnectionState(state);
        };
        signalRManager.onStateChange(handleStateChange);
        return () => { signalRManager.offStateChange(handleStateChange); };
    }, [hubName]);

    useEffect(() => {
        if (!user) return;

        let mounted = true;
        let retryTimeout: ReturnType<typeof setTimeout> | null = null;

        const tryConnect = async (attempt = 0) => {
            try {
                const conn = await signalRManager.connect(hubName);
                if (!mounted) {
                    signalRManager.disconnect(hubName);
                    return;
                }
                connectedRef.current = true;
                setConnectionState(conn.state);
            } catch (err) {
                console.error(`[useHub] Failed to connect to ${hubName} (attempt ${attempt + 1}):`, err);
                if (mounted && attempt < 5) {
                    const delay = Math.min(1000 * 2 ** attempt, 30000);
                    retryTimeout = setTimeout(() => tryConnect(attempt + 1), delay);
                } else if (mounted) {
                    setConnectionState(signalR.HubConnectionState.Disconnected);
                }
            }
        };

        tryConnect();

        return () => {
            mounted = false;
            if (retryTimeout) clearTimeout(retryTimeout);
            if (connectedRef.current) {
                signalRManager.disconnect(hubName);
                connectedRef.current = false;
            }
        };
    }, [hubName, user]);

    return useMemo(() => ({
        on: (event: string, handler: (...args: any[]) => void) =>
            signalRManager.on(hubName, event, handler as (...args: unknown[]) => void),
        off: (event: string, handler: (...args: any[]) => void) =>
            signalRManager.off(hubName, event, handler as (...args: unknown[]) => void),
        invoke: (method: string, ...args: any[]) =>
            signalRManager.invoke(hubName, method, ...args),
        onReconnected: (cb: () => void) => signalRManager.onReconnected(hubName, cb),
        offReconnected: (cb: () => void) => signalRManager.offReconnected(hubName, cb),
        connectionState,
    }), [hubName, connectionState]);
}
