using System;
using System.Collections.Generic;
using System.Text;

namespace GraphPrototype.BMP3
{
    public class FakeSensor : ISensor
    {
        /// <summary>
        /// The next reading the fake sensor will give
        /// </summary>
        public double NextReading { get; set; } = 1000;

        /// <summary>
        /// The maximum +/- change to make at each reading
        /// </summary>
        public double RateOfChange { get; set; } = 1;

        public double ReadPressure()
        {
            double reading = NextReading;

            // Add +/- RateOfChange to the reading to generate noise
            NextReading += (m_Generator.NextDouble() * (RateOfChange * 2)) - RateOfChange;

            return reading;
        }

        Random m_Generator = new Random();
        DateTime m_NextReadingTime = DateTime.UtcNow;
    }
}
