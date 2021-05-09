using System;
using System.Collections.Generic;
using System.Text;

namespace GraphPrototype.Clock
{
    public class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
