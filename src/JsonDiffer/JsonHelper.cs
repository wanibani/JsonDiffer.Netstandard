﻿using Newtonsoft.Json.Linq;

namespace JsonDiffer
{
    public static class JsonHelper
    {
        public static JToken Difference(this JToken first, JToken second, OutputMode outputMode = OutputMode.Symbol, bool showOriginalValues = false, string replaceValues = "")
        {
            return JsonDifferentiator.Differentiate(first, second, outputMode, showOriginalValues, replaceValues);
        }
    }
}