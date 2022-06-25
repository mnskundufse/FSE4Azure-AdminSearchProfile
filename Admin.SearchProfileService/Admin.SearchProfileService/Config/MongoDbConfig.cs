namespace Admin.SearchProfileService.Config
{
    public class MongoDbConfig
    {
        public string Database { get; set; }
        public string AzureConnectionString { get; set; }
        public string ConnectionString
        {
            get
            {
                return $@"mongodb://{AzureConnectionString}";
            }
        }
    }
}