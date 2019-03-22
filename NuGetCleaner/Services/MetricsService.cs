using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetCleaner.Services
{
    public class MetricsService
    {
        public void TrackException(Exception ex)
        {
            Analytics.TrackEvent("Exception", new Dictionary<string, string>
            {
                ["Name"] = ex.GetType().Name,
                ["StackTrace"] = ex.StackTrace,
                ["Message"] = ex.Message
            });
        }
    }
}
