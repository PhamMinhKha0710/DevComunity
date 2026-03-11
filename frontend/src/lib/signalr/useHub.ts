'use client';

import { useEffect, useRef, useState } from 'react';
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
        signalR.HubConnectionState.Disconnected
    );
    const connectedRef = useRef(false);

    useEffect(() => {
        if (!user) return;

        let mounted = true;

        (async () => {
            try {
                const conn = await signalRManager.connect(hubName);
                if (!mounted) {
                    signalRManager.disconnect(hubName);
                    return;
                }
                connectedRef.current = true;
                setConnectionState(conn.state);

                conn.onreconnecting(() => {
                    if (mounted) setConnectionState(signalR.HubConnectionState.Reconnecting);
                });
                conn.onreconnected(() => {
                    if (mounted) setConnectionState(signalR.HubConnectionState.Connected);
                });
                conn.onclose(() => {
                    if (mounted) setConnectionState(signalR.HubConnectionState.Disconnected);
                });
            } catch (err) {
                console.error(`[useHub] Failed to connect to ${hubName}:`, err);
                if (mounted) setConnectionState(signalR.HubConnectionState.Disconnected);
            }
        })();

        return () => {
            mounted = false;
            if (connectedRef.current) {
                signalRManager.disconnect(hubName);
                connectedRef.current = false;
            }
        };
    }, [hubName, user]);

    return {
        on: (event: string, handler: (...args: any[]) => void) =>
            signalRManager.on(hubName, event, handler as (...args: unknown[]) => void),
        off: (event: string, handler: (...args: any[]) => void) =>
            signalRManager.off(hubName, event, handler as (...args: unknown[]) => void),
        invoke: (method: string, ...args: any[]) =>
            signalRManager.invoke(hubName, method, ...args),
        connectionState,
    };
}
