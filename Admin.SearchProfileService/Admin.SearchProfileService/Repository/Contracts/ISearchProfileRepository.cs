using Admin.SearchProfileService.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Admin.SearchProfileService.Repository.Contracts
{
    public interface ISearchProfileRepository
    {
        Task<List<UserProfileForAdminDatabase>> GetAllUserProfileRepository();
        Task InsertUserProfileRepository(UserProfile userProfile);
        Task<bool> UpdateUserProfileRepository(UserProfile userProfile);
    }
}