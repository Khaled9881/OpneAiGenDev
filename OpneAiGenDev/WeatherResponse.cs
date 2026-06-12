using System;
using System.Collections.Generic;
using System.Text;

namespace OpneAiGenDev
{
    public class WeatherResponse
    {
        public string City { get; set; } = "";
        public double Temperature { get; set; }
        public string Condition { get; set; } = "";
        public int Humidity { get; set; }
        public string TravelAdvice { get; set; } = "";
    }
}
