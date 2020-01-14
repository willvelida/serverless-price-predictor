using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using ServerlessPricePredictor.ModelTrainer.Helpers;

namespace ServerlessPricePredictor.ModelTrainer.Functions
{
    public class ModelTrainer
    {
        private readonly ILogger<ModelTrainer> _logger;
        private readonly IConfiguration _config;
        private readonly MLContext _mlContext;
        private readonly IAzureStorageHelpers _azureStorageHelpers;

        public ModelTrainer(
            ILogger<ModelTrainer> logger,
            IConfiguration config,
            MLContext mlContext,
            IAzureStorageHelpers azureStorageHelpers)
        {
            _logger = logger;
            _config = config;
            _mlContext = mlContext;
            _azureStorageHelpers = azureStorageHelpers;
        }

        [FunctionName(nameof(ModelTrainer))]
        public void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Query Azure SQL dataset

            // Add Data to IDataView

            // Train Model

            // Upload to Azure Storage
        }
    }
}
