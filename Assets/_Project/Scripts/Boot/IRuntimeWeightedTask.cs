using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenCrowns.Boot
{
    /// <summary>
    /// Optional interface for preload tasks that can compute their weight at runtime
    /// (e.g., number of assets to load). If implemented, BootLoaderController uses
    /// GetRuntimeWeight() instead of the serialized Weight.
    /// </summary>
    public interface IRuntimeWeightedTask
    {
        float GetRuntimeWeight(); // Must be >= 1 in practice
    }
}
