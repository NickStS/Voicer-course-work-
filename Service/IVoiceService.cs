using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Voicer.Service
{
    public interface IVoiceService
    {
        Task<bool> ChangeUsernameAsync(string userId, string newUsername);
        Task<IdentityUser> GetUserByIdAsync(string userId);
        Task<string> ProcessQuery(string query);
    }
}
