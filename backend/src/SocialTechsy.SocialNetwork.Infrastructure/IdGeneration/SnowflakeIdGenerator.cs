namespace SocialTechsy.SocialNetwork.Infrastructure.IdGeneration;

/// <summary>
/// Distributed 64-bit ID generator based on Twitter's Snowflake algorithm.
/// Layout: 41 bits timestamp | 10 bits server_id | 13 bits sequence
/// Supports ~8192 IDs per millisecond per server, with 1024 server instances.
/// </summary>
public sealed class SnowflakeIdGenerator
{
    private const long CustomEpoch = 1735689600000L; // 2025-01-01T00:00:00Z
    private const int ServerIdBits = 10;
    private const int SequenceBits = 13;
    private const long MaxSequence = (1L << SequenceBits) - 1; // 8191
    private const long MaxServerId = (1L << ServerIdBits) - 1; // 1023

    private readonly int _serverId;
    private long _lastTimestamp = -1;
    private long _sequence;
    private readonly object _lock = new();

    public SnowflakeIdGenerator(int serverId)
    {
        if (serverId < 0 || serverId > MaxServerId)
            throw new ArgumentOutOfRangeException(nameof(serverId),
                $"Server ID must be between 0 and {MaxServerId}.");
        _serverId = serverId;
    }

    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = CurrentTimeMs();

            if (timestamp < _lastTimestamp)
                throw new InvalidOperationException(
                    $"Clock moved backwards. Refusing to generate ID for {_lastTimestamp - timestamp}ms.");

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                    timestamp = WaitNextMs(_lastTimestamp);
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - CustomEpoch) << (ServerIdBits + SequenceBits))
                   | ((long)_serverId << SequenceBits)
                   | _sequence;
        }
    }

    /// <summary>
    /// Extracts the UTC timestamp embedded in a Snowflake ID.
    /// </summary>
    public static DateTimeOffset ExtractTimestamp(long id)
    {
        var ms = (id >> (ServerIdBits + SequenceBits)) + CustomEpoch;
        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }

    private static long CurrentTimeMs() =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static long WaitNextMs(long lastTimestamp)
    {
        var timestamp = CurrentTimeMs();
        while (timestamp <= lastTimestamp)
            timestamp = CurrentTimeMs();
        return timestamp;
    }
}
