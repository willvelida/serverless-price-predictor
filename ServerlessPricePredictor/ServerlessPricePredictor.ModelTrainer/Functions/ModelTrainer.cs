using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using ServerlessPricePredictor.ModelTrainer.Helpers;
using ServerlessPricePredictor.ModelTrainer.Models;

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
        public async Task Run([TimerTrigger("5 * * * * *")]TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Authenticate to Azure Storage
            try
            {
                CloudBlobClient cloudBlobClient = _azureStorageHelpers.ConnectToBlobClient(_config[Settings.STORAGE_ACCOUNT_NAME], _config[Settings.STORAGE_ACCOUNT_KEY]);
                CloudBlobContainer modelCloudBlobContainer = _azureStorageHelpers.GetBlobContainer(cloudBlobClient, _config[Settings.MODEL_CONTAINER_NAME]);

                // Read File From Azure Storage
                string _trainDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-train.csv");
                string _testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-test.csv");
                string modelPath = _config[Settings.MODEL_PATH];

                // Add Data to IDataView and Train Model
                await TrainAndSaveModel(_mlContext, _trainDataPath, _testDataPath, modelPath, modelCloudBlobContainer);
                _logger.LogInformation($"The model has been uploaded to {modelCloudBlobContainer.Name}. Saved as {modelPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown: {ex.Message}");
                throw;
            }                
        }

        private async Task TrainAndSaveModel(MLContext mlContext, string trainFilePath, string testFilePath, string modelPath, CloudBlobContainer cloudBlobContainer)
        {
            // Read flat file from Azure Storage
            _logger.LogInformation("Loading the file into the pipeline");
            IDataView dataView = mlContext.Data.LoadFromTextFile<TaxiTrip>(trainFilePath, hasHeader: true, separatorChar: ',');

            // Create the pipeline
            _logger.LogInformation("Training pipeline");
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "FareAmount")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "VendorIdEncoded", inputColumnName: "VendorId"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RateCodeEncoded", inputColumnName: "RateCode"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PaymentTypeEncoded", inputColumnName: "PaymentType"))
                .Append(mlContext.Transforms.Concatenate("Features", "VendorIdEncoded", "RateCodeEncoded", "PassengerCount", "TripDistance", "PaymentTypeEncoded"))
                .Append(mlContext.Regression.Trainers.FastTree());

            // Fit the model
            _logger.LogInformation("Fitting model");
            var model = pipeline.Fit(dataView);

            // Test the model
            _logger.LogInformation("Evaluating the model");
            var modelRSquaredValue = Evaluate(_mlContext, model, testFilePath);
            _logger.LogInformation($"R-Squared value for model is {modelRSquaredValue}");

            if (modelRSquaredValue >= 0.7)
            {
                _logger.LogInformation("Good fit! Saving model");
                mlContext.Model.Save(model, dataView.Schema, modelPath);

                // Upload Model to Blob Storage
                _logger.LogInformation("Uploading model to Blob Storage");                
                await _azureStorageHelpers.UploadBlobToStorage(cloudBlobContainer, modelPath);
            }
            else
            {
                _logger.LogInformation("The model is a poor fit");
            }                    
        }

        private double Evaluate(MLContext mlContext, ITransformer model, string testFilePath)
        {
            IDataView dataView = mlContext.Data.LoadFromTextFile<TaxiTrip>(testFilePath, hasHeader: true, separatorChar: ',');

            var predictions = model.Transform(dataView);

            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");

            double rSquaredValue = metrics.RSquared;

            return rSquaredValue;
        }
    }
}
