using Admin.SearchProfileService.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Admin.SearchProfileService.Business.Contracts
{
    public interface ISearchProfileBusiness
    {
        bool ValidateRequest(string criteria, string criteriaValue, ref ApiResponse response);
        Task<List<UserProfileForAdminDatabase>> GetAllUserProfileBusiness();
    }
}