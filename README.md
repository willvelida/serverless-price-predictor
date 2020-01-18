# serverless-price-predictor

This repository contains two Azure Functions projects. The first trains a regression model developed using ML.NET and uploads the model to Azure Blob Storage. The second then uses this model in a Serverless API to create predictions and inserts those predictions into a Cosmos DB container. 


