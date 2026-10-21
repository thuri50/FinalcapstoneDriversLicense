using FinalcapstoneDriversLicense.Dtos.Auth;
using FinalcapstoneDriversLicense.Entities;

namespace FinalcapstoneDriversLicense.Service.IService
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task CreateTokenAsync(ApplicationUser user);
    }
}
