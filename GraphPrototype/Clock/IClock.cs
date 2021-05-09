using System;
using System.Collections.Generic;
using System.Text;

namespace GraphPrototype.Clock
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
