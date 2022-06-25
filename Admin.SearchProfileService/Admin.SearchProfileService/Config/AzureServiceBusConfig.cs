namespace Admin.SearchProfileService.Config
{
    public class AzureServiceBusConfig
    {
        public string AzureServiceBusConnectionString { get; set; }
        public string TopicNameToAddUserProfileSubscribe { get; set; }
        public string SubscriptionNameToAddUserProfile { get; set; }
        public string TopicNameToUpdateUserProfileSubscribe { get; set; }
        public string SubscriptionNameToUpdateUserProfile { get; set; }
    }
}