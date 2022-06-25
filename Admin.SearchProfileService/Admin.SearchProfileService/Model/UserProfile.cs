﻿using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Admin.SearchProfileService.Model
{
    public class UserProfile
    {
        [BsonId]
        public long UserId { get; set; }

        public string Name { get; set; }
        public string AssociateId { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }

        public TechnicalSkillExpertiseLevel TechnicalSkillExpertiseLevel { get; set; }
        public NonTechnicalSkillExpertiseLevel NonTechnicalSkillExpertiseLevel { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class TechnicalSkillExpertiseLevel
    {
        public string HTMLCSSJavaScriptExpertiseLevel { get; set; }
        public string AngularExpertiseLevel { get; set; }
        public string ReactExpertiseLevel { get; set; }
        public string AspNetCoreExpertiseLevel { get; set; }
        public string RestfulExpertiseLevel { get; set; }
        public string EntityFrameworkExpertiseLevel { get; set; }
        public string GitExpertiseLevel { get; set; }
        public string DockerExpertiseLevel { get; set; }
        public string JenkinsExpertiseLevel { get; set; }
        public string AzureExpertiseLevel { get; set; }
    }

    public class NonTechnicalSkillExpertiseLevel
    {
        public string SpokenExpertiseLevel { get; set; }
        public string CommunicationExpertiseLevel { get; set; }
        public string AptitudeExpertiseLevel { get; set; }
    }

    public class UserProfilesDetails
    {
        public int XTotal { get; set; }
        public int XTotalPages { get; set; }
        public int XPerPage { get; set; }
        public int XPage { get; set; }
        public IEnumerable<UserProfileForAdminDatabase> UserList { get; set; }
    }
}
