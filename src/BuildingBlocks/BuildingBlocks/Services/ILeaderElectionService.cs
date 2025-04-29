using System.Threading.Tasks;

namespace BuildingBlocks.Services
{
    /// <summary>
    /// Interface cho dịch vụ Leader Election
    /// </summary>
    public interface ILeaderElectionService
    {
        /// <summary>
        /// Kiểm tra xem instance hiện tại có phải là leader không
        /// </summary>
        /// <returns>True nếu instance hiện tại là leader, ngược lại là false</returns>
        Task<bool> IsLeaderAsync();
    }
}