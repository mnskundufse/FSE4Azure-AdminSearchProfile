using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Admin.SearchProfileService.Business.Contracts;
using Admin.SearchProfileService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Admin.SearchProfileService.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("skill-tracker/api/v{version:apiVersion}/admin")]
    [Produces("application/json")]
    public class SearchProfileController : ControllerBase
    {
        private readonly ILogger<SearchProfileController> _logger;
        private readonly ISearchProfileBusiness _showProfileBC;
        private readonly Cache _cache;

        public SearchProfileController(ILogger<SearchProfileController> logger, IDistributedCache cacheProvider, ISearchProfileBusiness showProfileBC)
        {
            _showProfileBC = showProfileBC;
            _logger = logger;
            _cache = new Cache(cacheProvider);
        }

        [MapToApiVersion("1.0")]
        [Authorize]
        [HttpGet("{criteria}/{criteriaValue}/{perPage?}/{page?}")]
        public async Task<IActionResult> GetEngineerProfilesDetails(string criteria, string criteriaValue, int? perPage, int? page)
        {
            ApiResponse response = new ApiResponse();
            if (_showProfileBC.ValidateRequest(criteria, criteriaValue, ref response))
            {
                response = await GetDesiredRecords(criteria, criteriaValue, perPage, page);
            }
            _logger.LogInformation("{date} : GetEngineerProfilesDetails of the SearchProfileController executed.", DateTime.UtcNow);
            return StatusCode(200, response);
        }

        /// <summary>
        /// Get Desired Records
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="criteriaValue"></param>
        /// <param name="perPage"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        private async Task<ApiResponse> GetDesiredRecords(string criteria, string criteriaValue, int? perPage, int? page)
        {
            try
            {
                List<UserProfileForAdminDatabase> userProfileList = await GetAllUserProfileDetails();
                UserProfilesDetails userProfileDetails = null;

                if (userProfileList != null && userProfileList.Count > 0)
                {
                    if (string.Compare(criteria.ToUpper(), "NAME") == 0)
                    {
                        List<UserProfileForAdminDatabase> userlist = userProfileList.FindAll(f => f.Name.ToUpper().Contains(criteriaValue.ToUpper()));
                        if (userlist != null && userlist.Count > 0)
                            userProfileDetails = GetPage(userlist, perPage, page);
                    }
                    else if (string.Compare(criteria.ToUpper(), "ASSOCIATE ID") == 0)
                    {
                        List<UserProfileForAdminDatabase> userlist = userProfileList.FindAll(f => f.AssociateId.Contains(criteriaValue.ToUpper()));
                        if (userlist != null && userlist.Count > 0)
                            userProfileDetails = GetPage(userlist, perPage, page);
                    }
                    else if (string.Compare(criteria.ToUpper(), "SKILL") == 0)
                    {
                        List<UserProfileForAdminDatabase> userlist = userProfileList.FindAll
                            (f =>
                                f.TechnicalSkillDetails.Any(o => o.SkillName.Equals(criteriaValue.ToUpper()) && o.SkillValue > 10) ||
                                f.NonTechnicalSkillDetails.Any(o => o.SkillName.Equals(criteriaValue.ToUpper()) && o.SkillValue > 10)
                            );
                        if (userlist != null && userlist.Count > 0)
                            userProfileDetails = GetPage(userlist, perPage, page);
                    }
                }

                ApiResponse response = new ApiResponse();
                if (userProfileDetails != null && userProfileDetails.XTotal > 0)
                {
                    userProfileDetails.UserList.ToList().ForEach(u =>
                    {
                        u.TechnicalSkillDetails = u.TechnicalSkillDetails.OrderByDescending(o => Convert.ToInt32(o.SkillValue)).ToList();
                        u.NonTechnicalSkillDetails = u.NonTechnicalSkillDetails.OrderByDescending(o => Convert.ToInt32(o.SkillValue)).ToList();
                    });
                    response.Result = userProfileDetails;
                }
                else
                {
                    response.Result = null;
                    response.Status.Message = "NO RECORD FOUND";
                    response.Status.IsValid = false;
                    response.Status.Status = "FAIL";
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unknown error occurred on the GetEngineerProfilesDetails of the SearchProfileController.");
                throw ex;
            }
        }

        /// <summary>
        /// Get Pagewise Desired Records
        /// </summary>
        /// <param name="list"></param>
        /// <param name="perPage"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        private UserProfilesDetails GetPage(List<UserProfileForAdminDatabase> list, int? perPage, int? page)
        {
            int totalDocuments = list.Count;
            var totalPages = Math.Ceiling((double)totalDocuments / perPage.Value);

            UserProfilesDetails userProfilesDetails;
            if (perPage.HasValue && perPage.Value > 0 && page.HasValue && page.Value > 0)
            {
                userProfilesDetails = new UserProfilesDetails()
                {
                    XTotal = totalDocuments,
                    XTotalPages = (int)totalPages,
                    XPerPage = perPage.Value,
                    XPage = page.Value,
                    UserList = list.Skip((page.Value - 1) * perPage.Value).Take(perPage.Value).ToList(),
                };
            }
            else
            {
                userProfilesDetails = new UserProfilesDetails()
                {
                    XTotal = totalDocuments,
                    XTotalPages = 1,
                    XPerPage = 0,
                    XPage = 0,
                    UserList = list
                };
            }
            return userProfilesDetails;
        }

        #region Queing to Cache
        /// <summary>
        /// Read from Cache if Key exist else read from Mongo DB
        /// </summary>
        /// <param name="userProfile"></param>
        private async Task<List<UserProfileForAdminDatabase>> GetAllUserProfileDetails()
        {
            List<UserProfileForAdminDatabase> userProfileList = await _cache.Get<List<UserProfileForAdminDatabase>>("UserProfiles");
            if (userProfileList == null)
            {
                userProfileList = await _showProfileBC.GetAllUserProfileBusiness();
                await _cache.Set<List<UserProfileForAdminDatabase>>("UserProfiles", userProfileList, new DistributedCacheEntryOptions());
            }
            return userProfileList;
        }
        #endregion
    }
}