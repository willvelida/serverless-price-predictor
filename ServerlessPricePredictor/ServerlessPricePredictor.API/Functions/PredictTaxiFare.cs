using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using ServerlessPricePredictor.API.Helpers;
using ServerlessPricePredictor.API.Models;
using Microsoft.Extensions.ML;

namespace ServerlessPricePredictor.API.Functions
{
    public class PredictTaxiFare
    {
        private readonly ILogger _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;
        private readonly PredictionEnginePool<TaxiTrip, TaxiTripFarePrediction> _predictionEnginePool;

        private Database _database;
        private Container _container;

        public PredictTaxiFare(
            ILogger<PredictTaxiFare> logger,
            CosmosClient cosmosClient,
            IConfiguration config,
            PredictionEnginePool<TaxiTrip, TaxiTripFarePrediction> predictionEnginePool)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
            _predictionEnginePool = predictionEnginePool;

            _database = _cosmosClient.GetDatabase(_config[Settings.DATABASE_NAME]);
            _container = _database.GetContainer(config[Settings.CONTAINER_NAME]);
        }

        [FunctionName(nameof(PredictTaxiFare))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "PredictTaxiFare")] HttpRequest req)
        {
            IActionResult returnValue = null;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var input = JsonConvert.DeserializeObject<TaxiTrip>(requestBody);

            // Make Prediction
            TaxiTripFarePrediction prediction = _predictionEnginePool.Predict(
                modelName: "TaxiTripModel",
                example: input);

            var insertedPrediction = new TaxiTripInsertObject
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = input.VendorId,
                RateCode = input.RateCode,
                PassengerCount = input.PassengerCount,
                TripTime = input.TripTime,
                TripDistance = input.TripDistance,
                PaymentType = input.PaymentType,
                FareAmount = input.FareAmount,
                PredictedFareAmount = prediction.FareAmount
            };

            try
            {
                ItemResponse<TaxiTripInsertObject> predictionResponse = await _container.CreateItemAsync(
                    insertedPrediction,
                    new PartitionKey(insertedPrediction.VendorId));

                returnValue = new OkObjectResult(predictionResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Inserting prediction failed: Exception thrown: {ex.Message}");
                returnValue = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            
            return returnValue;
        }
    }
}
