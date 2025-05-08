using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BuildingBlocks.Services
{
    /// <summary>
    /// Implementation of ILeaderElectionService that always returns true for IsLeaderAsync().
    /// Used in Production environment to disable leader election functionality.
    /// </summary>
    public class NoOpLeaderElectionService : ILeaderElectionService
    {
        private readonly ILogger<NoOpLeaderElectionService> _logger;

        public NoOpLeaderElectionService(ILogger<NoOpLeaderElectionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Always returns true, effectively making every instance a "leader"
        /// </summary>
        public Task<bool> IsLeaderAsync()
        {
            _logger.LogDebug("NoOpLeaderElectionService: Always returning true (leader election disabled)");
            return Task.FromResult(true);
        }
    }
}