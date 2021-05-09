using System;
using System.Collections.Generic;
using System.Text;

namespace GraphPrototype.Clock
{
    public class FakeClock : IClock
    {
        public double SpeedOfTime { get; set; } = 100;

        public DateTime Now
        {
            get
            {
                DateTime now = DateTime.Now;
                TimeSpan elapsedTime = now - m_LastReadingTime;
                elapsedTime *= SpeedOfTime;
                m_LastNowTime += elapsedTime;
                m_LastReadingTime = now;
                return m_LastNowTime;
            }
        }

        DateTime m_LastReadingTime = DateTime.Now;
        DateTime m_LastNowTime = DateTime.Now;
    }
}
