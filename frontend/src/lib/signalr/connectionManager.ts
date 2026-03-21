import * as signalR from '@microsoft/signalr';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5122';

type HubName = 'chat' | 'notifications' | 'presence' | 'question' | 'call' | 'activity';

type StateChangeCallback = (hubName: HubName, state: signalR.HubConnectionState) => void;

interface ManagedConnection {
    connection: signalR.HubConnection;
    refCount: number;
}

class SignalRConnectionManager {
    private connections = new Map<HubName, ManagedConnection>();
    private eventListeners = new Map<string, Set<(...args: unknown[]) => void>>();
    private reconnectCallbacks = new Map<HubName, Set<() => void>>();
    private stateChangeListeners = new Set<StateChangeCallback>();

    getConnection(hubName: HubName): signalR.HubConnection | null {
        return this.connections.get(hubName)?.connection ?? null;
    }

    onStateChange(callback: StateChangeCallback): void {
        this.stateChangeListeners.add(callback);
    }

    offStateChange(callback: StateChangeCallback): void {
        this.stateChangeListeners.delete(callback);
    }

    private notifyStateChange(hubName: HubName, state: signalR.HubConnectionState): void {
        this.stateChangeListeners.forEach(cb => cb(hubName, state));
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
                this.notifyStateChange(hubName, signalR.HubConnectionState.Connected);
            }
            return existing.connection;
        }

        const isDev = process.env.NODE_ENV === 'development';

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/${hubName}`, {
                accessTokenFactory: () => localStorage.getItem('accessToken') || '',
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    // Exponential backoff: 0, 2, 5, 10, 30 seconds
                    if (retryContext.previousRetryCount < 4) {
                        return [0, 2000, 5000, 10000, 30000][retryContext.previousRetryCount];
                    }
                    return 30000;
                }
            })
            .configureLogging(isDev ? signalR.LogLevel.Information : signalR.LogLevel.Warning)
            .withStatefulReconnect()
            .build();

        this.connections.set(hubName, { connection, refCount: 1 });

        connection.onreconnecting(() => {
            console.log(`[SignalR] ${hubName} reconnecting...`);
            this.notifyStateChange(hubName, signalR.HubConnectionState.Reconnecting);
        });

        connection.onreconnected(() => {
            console.log(`[SignalR] ${hubName} reconnected`);
            this.replayPendingHandlers(hubName, connection);
            this.notifyStateChange(hubName, signalR.HubConnectionState.Connected);
            this.reconnectCallbacks.get(hubName)?.forEach(cb => cb());
        });

        connection.onclose(() => {
            console.log(`[SignalR] ${hubName} connection closed`);
            this.notifyStateChange(hubName, signalR.HubConnectionState.Disconnected);
        });

        await connection.start();
        this.replayPendingHandlers(hubName, connection);
        console.log(`[SignalR] ${hubName} connected`);
        this.notifyStateChange(hubName, signalR.HubConnectionState.Connected);
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

    private replayPendingHandlers(hubName: HubName, connection: signalR.HubConnection): void {
        for (const [key, handlers] of this.eventListeners.entries()) {
            if (!key.startsWith(`${hubName}:`)) continue;
            const event = key.substring(hubName.length + 1);
            handlers.forEach(handler => {
                connection.off(event, handler);
                connection.on(event, handler);
            });
        }
    }

    on(hubName: HubName, event: string, handler: (...args: unknown[]) => void): void {
        const key = `${hubName}:${event}`;
        if (!this.eventListeners.has(key)) {
            this.eventListeners.set(key, new Set());
        }
        this.eventListeners.get(key)!.add(handler);

        const conn = this.getConnection(hubName);
        if (conn) {
            conn.on(event, handler);
        }
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

// Preserve singleton across Fast Refresh / HMR in development
function getOrCreateManager(): SignalRConnectionManager {
    if (typeof window !== 'undefined') {
        const win = window as any;
        if (win.__signalRManager instanceof SignalRConnectionManager) {
            return win.__signalRManager;
        }
        const manager = new SignalRConnectionManager();
        win.__signalRManager = manager;
        return manager;
    }
    return new SignalRConnectionManager();
}

export const signalRManager = getOrCreateManager();
export type { HubName };
