import * as signalR from '@microsoft/signalr';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';

type HubName = 'chat' | 'notifications' | 'presence' | 'question' | 'call' | 'activity';

interface ManagedConnection {
    connection: signalR.HubConnection;
    refCount: number;
}

class SignalRConnectionManager {
    private connections = new Map<HubName, ManagedConnection>();
    private eventListeners = new Map<string, Set<(...args: unknown[]) => void>>();
    private reconnectCallbacks = new Map<HubName, Set<() => void>>();

    getConnection(hubName: HubName): signalR.HubConnection | null {
        return this.connections.get(hubName)?.connection ?? null;
    }

    async connect(hubName: HubName): Promise<signalR.HubConnection> {
        const existing = this.connections.get(hubName);
        if (existing) {
            existing.refCount++;
            if (existing.connection.state === signalR.HubConnectionState.Connected) {
                return existing.connection;
            }
            if (existing.connection.state === signalR.HubConnectionState.Disconnected) {
                await existing.connection.start();
            }
            return existing.connection;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/${hubName}`, {
                accessTokenFactory: () => localStorage.getItem('accessToken') || '',
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.connections.set(hubName, { connection, refCount: 1 });

        connection.onclose(() => {
            console.log(`[SignalR] ${hubName} connection closed`);
        });

        connection.onreconnected(() => {
            console.log(`[SignalR] ${hubName} reconnected`);
            this.reconnectCallbacks.get(hubName)?.forEach(cb => cb());
        });

        await connection.start();
        console.log(`[SignalR] ${hubName} connected`);
        return connection;
    }

    async disconnect(hubName: HubName): Promise<void> {
        const managed = this.connections.get(hubName);
        if (!managed) return;

        managed.refCount--;
        if (managed.refCount <= 0) {
            if (managed.connection.state === signalR.HubConnectionState.Connected) {
                await managed.connection.stop();
            }
            this.connections.delete(hubName);
            console.log(`[SignalR] ${hubName} disconnected and removed`);
        }
    }

    async disconnectAll(): Promise<void> {
        const stops = Array.from(this.connections.entries()).map(async ([name, managed]) => {
            if (managed.connection.state === signalR.HubConnectionState.Connected) {
                await managed.connection.stop();
            }
            console.log(`[SignalR] ${name} disconnected`);
        });
        await Promise.all(stops);
        this.connections.clear();
        this.eventListeners.clear();
        this.reconnectCallbacks.clear();
    }

    onReconnected(hubName: HubName, callback: () => void): void {
        if (!this.reconnectCallbacks.has(hubName)) {
            this.reconnectCallbacks.set(hubName, new Set());
        }
        this.reconnectCallbacks.get(hubName)!.add(callback);
    }

    offReconnected(hubName: HubName, callback: () => void): void {
        this.reconnectCallbacks.get(hubName)?.delete(callback);
    }

    on(hubName: HubName, event: string, handler: (...args: unknown[]) => void): void {
        const conn = this.getConnection(hubName);
        if (conn) {
            conn.on(event, handler);
        }
        const key = `${hubName}:${event}`;
        if (!this.eventListeners.has(key)) {
            this.eventListeners.set(key, new Set());
        }
        this.eventListeners.get(key)!.add(handler);
    }

    off(hubName: HubName, event: string, handler: (...args: unknown[]) => void): void {
        const conn = this.getConnection(hubName);
        if (conn) {
            conn.off(event, handler);
        }
        const key = `${hubName}:${event}`;
        this.eventListeners.get(key)?.delete(handler);
    }

    async invoke(hubName: HubName, method: string, ...args: unknown[]): Promise<unknown> {
        const conn = this.getConnection(hubName);
        if (!conn || conn.state !== signalR.HubConnectionState.Connected) {
            throw new Error(`[SignalR] ${hubName} not connected`);
        }
        return conn.invoke(method, ...args);
    }

    getState(hubName: HubName): signalR.HubConnectionState {
        return this.getConnection(hubName)?.state ?? signalR.HubConnectionState.Disconnected;
    }
}

export const signalRManager = new SignalRConnectionManager();
export type { HubName };
