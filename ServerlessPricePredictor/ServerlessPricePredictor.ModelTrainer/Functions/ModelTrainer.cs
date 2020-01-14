using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ServerlessPricePredictor.ModelTrainer.Functions
{
    public class ModelTrainer
    {
        [FunctionName(nameof(ModelTrainer))]
        public void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Query Azure SQL dataset

            // Add Data to IDataView

            // Train Model

            // Upload to Azure Storage
        }
    }
}
