---
name: Fix Incoming Call Notifications
overview: "Fix two issues preventing the receiver from knowing about incoming calls: (1) the call hub connection has no retry logic, so if it fails on first attempt it stays dead; (2) there is no ringtone/sound for incoming calls."
todos:
  - id: retry-connect
    content: Add retry with exponential backoff to useHub.ts for initial connection failures
    status: completed
  - id: incoming-ringtone
    content: Add synthesized ringtone sound in CallOverlay when callState is incoming
    status: completed
isProject: false
---

# Fix Incoming Call Notifications

## Root Cause Analysis

The receiver does not see incoming calls because of two problems:

### Problem 1: No retry for initial SignalR connection

In [frontend/src/lib/signalr/useHub.ts](frontend/src/lib/signalr/useHub.ts), the `connect()` call at line 28 fires once. If it fails (e.g. the backend hadn't restarted yet with the new CallHub), the catch block at line 45 just logs the error and sets state to `Disconnected`. There is **no retry**. The `withAutomaticReconnect` configured on the connection builder only applies *after* a successful initial connection -- it does NOT retry a failed `start()`.

This means if User 2 loaded the chat page before the backend was restarted with CallHub, their `call` hub stays permanently disconnected. The `IncomingCall` event is sent by the backend but has nowhere to deliver it.

### Problem 2: No ringtone for incoming calls

Even when the event does arrive, the [CallOverlay.tsx](frontend/src/components/chat/CallOverlay.tsx) only shows a visual pulse animation. There is no audio cue to alert the user.

---

## Fix 1: Add retry with exponential backoff to `useHub`

**File:** [frontend/src/lib/signalr/useHub.ts](frontend/src/lib/signalr/useHub.ts)

Replace the single `connect()` attempt with a retry loop (max 5 retries, exponential backoff: 1s, 2s, 4s, 8s, 16s):

```typescript
const tryConnect = async (attempt = 0) => {
    try {
        const conn = await signalRManager.connect(hubName);
        if (!mounted) { signalRManager.disconnect(hubName); return; }
        connectedRef.current = true;
        setConnectionState(conn.state);
        // register reconnect/close handlers...
    } catch (err) {
        console.error(`[useHub] Failed to connect to ${hubName} (attempt ${attempt + 1}):`, err);
        if (mounted && attempt < 5) {
            retryTimeout = setTimeout(() => tryConnect(attempt + 1), Math.min(1000 * 2 ** attempt, 30000));
        } else if (mounted) {
            setConnectionState(signalR.HubConnectionState.Disconnected);
        }
    }
};
tryConnect();
```

Also add `clearTimeout(retryTimeout)` in the cleanup function to prevent retries after unmount.

## Fix 2: Add incoming call ringtone

**File:** [frontend/src/components/chat/CallOverlay.tsx](frontend/src/components/chat/CallOverlay.tsx)

Add an `Audio` element that plays a ringtone loop when `callState === 'incoming'`:

- Use the Web Audio API or a simple `<audio>` element with a generated ringtone tone (oscillator-based, no external file needed)
- Play on mount when state is `incoming`, stop when state changes
- Use a `useEffect` that starts/stops the audio based on `callState`

Alternatively, use a simple synthesized ring pattern via `AudioContext` + `OscillatorNode` to avoid needing an audio file:

```typescript
useEffect(() => {
    if (callState !== 'incoming') return;
    const ctx = new AudioContext();
    let interval: NodeJS.Timeout;
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
    return () => { clearInterval(interval); ctx.close(); };
}, [callState]);
```

This creates a short 440Hz beep every 1.5 seconds while the call is incoming.
