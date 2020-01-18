using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessPricePredictor.API.Models
{
    public class TaxiTripInsertObject
    {
        [JsonProperty("id")]
        public string Id;

        public string VendorId;
       
        public string RateCode;
       
        public float PassengerCount;
       
        public float TripTime;
     
        public float TripDistance;
       
        public string PaymentType;
        
        public float FareAmount;

        public float PredictedFareAmount;
    }
}
