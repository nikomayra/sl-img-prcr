using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace sl_img_prcr.Services
{
    public class RateLimitingService
    {
        private readonly ILogger<RateLimitingService> _logger;
        private readonly ConcurrentDictionary<string, ClientRateLimit> _clientRateLimits = new();
        
        // Define rate limits
        private readonly int _maxRequestsPerMinute = 10;
        private readonly int _maxRequestsPerHour = 30;
        private readonly int _maxRequestsPerDay = 100;

        public RateLimitingService(ILogger<RateLimitingService> logger)
        {
            _logger = logger;
        }

        public bool IsClientAllowed(string clientIp)
        {
            var now = DateTimeOffset.UtcNow;
            var clientLimit = _clientRateLimits.GetOrAdd(clientIp, _ => new ClientRateLimit());

            // Clean up old request timestamps periodically (every 100 requests)
            if (clientLimit.TotalRequests % 100 == 0)
            {
                CleanupOldRequests(clientLimit, now);
            }

            // Track this request
            clientLimit.RequestTimestamps.Add(now);
            Interlocked.Increment(ref clientLimit.TotalRequests);
            
            // Check rate limits
            int requestsLastMinute = CountRequestsInTimeWindow(clientLimit, now.AddMinutes(-1));
            int requestsLastHour = CountRequestsInTimeWindow(clientLimit, now.AddHours(-1));
            int requestsLastDay = CountRequestsInTimeWindow(clientLimit, now.AddDays(-1));

            bool isAllowed = 
                requestsLastMinute <= _maxRequestsPerMinute && 
                requestsLastHour <= _maxRequestsPerHour &&
                requestsLastDay <= _maxRequestsPerDay;

            if (!isAllowed)
            {
                _logger.LogWarning($"Rate limit exceeded for IP {clientIp}. " +
                                  $"Requests in last minute: {requestsLastMinute}, " +
                                  $"hour: {requestsLastHour}, day: {requestsLastDay}");
            }

            return isAllowed;
        }

        private int CountRequestsInTimeWindow(ClientRateLimit clientLimit, DateTimeOffset cutoff)
        {
            return clientLimit.RequestTimestamps.Count(ts => ts >= cutoff);
        }

        private void CleanupOldRequests(ClientRateLimit clientLimit, DateTimeOffset now)
        {
            // Remove timestamps older than 1 day
            var cutoff = now.AddDays(-1);
            clientLimit.RequestTimestamps.RemoveAll(ts => ts < cutoff);
        }

        private class ClientRateLimit
        {
            public List<DateTimeOffset> RequestTimestamps { get; } = new List<DateTimeOffset>();
            public long TotalRequests;
        }
    }
} 