
using FinalcapstoneDriversLicense.Dtos.Application;
using FinalcapstoneDriversLicense.Entities;

namespace FinalcapstoneDriversLicense.Services;

public interface IApplicationService
{
    Task<ApplicationResponse> CreateAsync(Guid userId, CreateApplicationRequest request);
    Task<ApplicationResponse> GetMineAsync(Guid userId);
    Task<ApplicationResponse> GetByIdAsync(int id);
    Task<IReadOnlyList<ApplicationResponse>> GetAllAsync(string? status);
    Task<ApplicationResponse> UpdateStatusAsync(int id, Guid staffUserId, UpdateApplicationStatusRequest request);
    Task<ApplicationResponse> CollectAsync(int id, Guid staffUserId);
    Task<IReadOnlyList<StatusHistoryResponse>> GetHistoryAsync(int id);

        // Update any other methods like UpdateStatusAsync if they now use Guid as well:
        Task UpdateStatusAsync(int id, ApplicationStatus requestedStatus, Guid staffUserId, string? comment = null);
  
    
        


}
