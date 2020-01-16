using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessPricePredictor.API.Models
{
    public class TaxiTripFarePrediction
    {
        [ColumnName("Score")]
        public float FareAmount;
    }
}
