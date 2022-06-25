using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Admin.SearchProfileService.Config;
using Admin.SearchProfileService.Model;
using Admin.SearchProfileService.Repository.Contracts;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Admin.SearchProfileService.Service
{
    public class AdminSearchProfileService : BackgroundService
    {

        public AdminSearchProfileService(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(
                "AdminSearchProfileService: Consume Scoped Service Hosted Service running.");

            await StartConsume(stoppingToken);
        }

        private async Task StartConsume(CancellationToken stoppingToken)
        {
            Console.WriteLine(
                "AdminSearchProfileService: Consume Scoped Service Hosted Service is working.");

            using (var scope = Services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<IScopedProcessingService>();

                await scopedProcessingService.StartConsume(stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(
                "AdminSearchProfileService: Consume Scoped Service Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }

    internal interface IScopedProcessingService
    {
        Task StartConsume(CancellationToken stoppingToken);
    }

    internal class ScopedProcessingService : IScopedProcessingService
    {
        private int executionCount = 0;

        private readonly ISearchProfileRepository _repo;
        private readonly ILogger<AdminSearchProfileService> _logger;
        private readonly AzureServiceBusConfig _azureServiceBusConfig;

        public ScopedProcessingService(ILogger<AdminSearchProfileService> logger, IOptions<AzureServiceBusConfig> azureServiceBusConfig, ISearchProfileRepository repo)
        {
            _repo = repo;
            _logger = logger;
            _azureServiceBusConfig = azureServiceBusConfig.Value;
        }

        // Handle received messages for add UserProfile
        async Task AddUserProfileMessageHandler(ProcessMessageEventArgs args)
        {
            string userRequest = Encoding.UTF8.GetString(args.Message.Body);

            #region Insert-UserProfile (Admin-Service)
            if (!string.IsNullOrWhiteSpace(userRequest))
            {
                //Deserilaize 
                UserProfile userProfileForInsert = JsonConvert.DeserializeObject<UserProfile>(userRequest);
                
                try
                {
                    await _repo.InsertUserProfileRepository(userProfileForInsert);
                    string message = "Status Code:200, Message:UserProfile Record inseterd successfully, Operation:AdminSearchProfileService consumer.";
                    _logger.LogInformation("{date} : InsertUserProfile of AdminSearchProfileService consumer operation executed. Message:{message}.", DateTime.UtcNow, message);
                    Console.WriteLine(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unknown error occurred on the InsertUserProfile of AdminSearchProfileService consumer operation.");
                    Console.WriteLine("Status Code:0,  Message:Unknown Exception Occured, Operation:AdminSearchProfileService consumer, Exception:" + ex);
                }
            }
            #endregion

            // complete the message. messages is deleted from the subscription. 
            await args.CompleteMessageAsync(args.Message);
        }

        // Handle received messages for update bid
        async Task UpdateUserProfileMessageHandler(ProcessMessageEventArgs args)
        {
            string userRequest = Encoding.UTF8.GetString(args.Message.Body);

            #region Update-UserProfile (Admin-Service)
            if (!string.IsNullOrWhiteSpace(userRequest))
            {
                UserProfile userProfileForUpdate = JsonConvert.DeserializeObject<UserProfile>(userRequest);

                try
                {
                    bool updateStatus = await _repo.UpdateUserProfileRepository(userProfileForUpdate);

                    if (updateStatus)
                    {
                        string message = "Status Code:200, Message:UserProfile Record updated successfully, Operation:AdminSearchProfileService consumer.";
                        _logger.LogInformation("{0} : UpdateUserProfile of AdminSearchProfileService consumer operation executed. Message:{1}.", DateTime.UtcNow, message);
                        Console.WriteLine(message);
                    }
                    else
                    {
                        string message = "Status Code:405, Message:UserProfile Record update failed, Operation:AdminSearchProfileService consumer.";
                        _logger.LogInformation("{date} : UpdateUserProfile of AdminSearchProfileService consumer operation executed. Message:{message}.", DateTime.UtcNow, message);
                        Console.WriteLine(message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unknown error occurred on the UpdateUserProfile of AdminSearchProfileService consumer operation.");
                    Console.WriteLine("Status Code:0, Message:Unknown Exception Occured, Operation:AdminSearchProfileService consumer, Exception:" + ex);
                }
            }
            #endregion

            // complete the message. messages is deleted from the subscription. 
            await args.CompleteMessageAsync(args.Message);
        }

        // Handle any errors when receiving messages
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        public async Task StartConsume(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                executionCount++;

                Console.WriteLine(
                   "AdminSearchProfileService: Scoped Processing Service is working. Count: {0}", executionCount);

                #region Insert-UserProfile (Admin-Service)
                // Create the clients that we'll use for sending and processing messages.
                ServiceBusClient addUserProfileclient = new ServiceBusClient(_azureServiceBusConfig.AzureServiceBusConnectionString);

                // create a processor that we can use to process the messages
                ServiceBusProcessor addUserProfileProcessor = addUserProfileclient.CreateProcessor(_azureServiceBusConfig.TopicNameToAddUserProfileSubscribe, _azureServiceBusConfig.SubscriptionNameToAddUserProfile, new ServiceBusProcessorOptions());

                try
                {
                    // add handler to process messages
                    addUserProfileProcessor.ProcessMessageAsync += AddUserProfileMessageHandler;

                    // add handler to process any errors
                    addUserProfileProcessor.ProcessErrorAsync += ErrorHandler;

                    // start processing 
                    await addUserProfileProcessor.StartProcessingAsync();

                    await Task.Delay(6000);

                    // stop processing 
                    Console.WriteLine("\nStopping the receiver...");
                    await addUserProfileProcessor.StopProcessingAsync();
                    Console.WriteLine("Stopped receiving messages");
                }
                finally
                {
                    // Calling DisposeAsync on client types is required to ensure that network
                    // resources and other unmanaged objects are properly cleaned up.
                    await addUserProfileProcessor.DisposeAsync();
                    await addUserProfileclient.DisposeAsync();
                }
                #endregion

                #region Update-UserProfile (Admin-Service)
                // Create the clients that we'll use for sending and processing messages.
                ServiceBusClient updateUserProfileclient = new ServiceBusClient(_azureServiceBusConfig.AzureServiceBusConnectionString);

                // create a processor that we can use to process the messages
                ServiceBusProcessor updateUserProfileProcessor = updateUserProfileclient.CreateProcessor(_azureServiceBusConfig.TopicNameToUpdateUserProfileSubscribe, _azureServiceBusConfig.SubscriptionNameToUpdateUserProfile, new ServiceBusProcessorOptions());

                try
                {
                    // add handler to process messages
                    updateUserProfileProcessor.ProcessMessageAsync += UpdateUserProfileMessageHandler;

                    // add handler to process any errors
                    updateUserProfileProcessor.ProcessErrorAsync += ErrorHandler;

                    // start processing 
                    await updateUserProfileProcessor.StartProcessingAsync();

                    await Task.Delay(6000);

                    // stop processing 
                    Console.WriteLine("\nStopping the receiver...");
                    await updateUserProfileProcessor.StopProcessingAsync();
                    Console.WriteLine("Stopped receiving messages");
                }
                finally
                {
                    // Calling DisposeAsync on client types is required to ensure that network
                    // resources and other unmanaged objects are properly cleaned up.
                    await updateUserProfileProcessor.DisposeAsync();
                    await updateUserProfileclient.DisposeAsync();
                }
                #endregion

                await Task.Delay(100, stoppingToken);
            }
        }
    }
}