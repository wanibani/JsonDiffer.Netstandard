using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace JsonDiffer
{
    public static class JsonHelper
    {
        public static JToken Difference(this JToken first, JToken second, OutputMode outputMode = OutputMode.Symbol, bool showOriginalValues = false, List<string> hidePropertyValues = default)
        {
            return JsonDifferentiator.Differentiate(first, second, outputMode, showOriginalValues, hidePropertyValues);
        }
    }
}