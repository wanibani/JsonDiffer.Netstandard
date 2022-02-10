﻿using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace JsonDiffer
{
    public class JsonDifferentiator
    {
        public OutputMode OutputMode { get; private set; }
        public bool ShowOriginalValues { get; private set; }
        public string ReplaceValues { get; private set; }

        public JsonDifferentiator(OutputMode outputMode, bool showOriginalValues, string replaceValues)
        {
            this.OutputMode = outputMode;
            this.ShowOriginalValues = showOriginalValues;
            this.ReplaceValues = replaceValues;
        }

        private static TargetNode PointTargetNode(JToken diff, string property, ChangeMode mode, OutputMode outMode)
        {
            string symbol = string.Empty;

            switch (mode)
            {
                case ChangeMode.Changed:
                    symbol = outMode == OutputMode.Symbol ? $"*{property}" : "changed";
                    break;

                case ChangeMode.Added:
                    symbol = outMode == OutputMode.Symbol ? $"+{property}" : "added";
                    break;

                case ChangeMode.Removed:
                    symbol = outMode == OutputMode.Symbol ? $"-{property}" : "removed";
                    break;
            }

            if (outMode == OutputMode.Detailed && diff[symbol] == null)
            {
                diff[symbol] = JToken.Parse("{}");
            }

            return new TargetNode(symbol, (outMode == OutputMode.Symbol) ? null : property);

        }

        public static JToken Differentiate(JToken first, JToken second, OutputMode outputMode = OutputMode.Symbol, bool showOriginalValues = false, string replaceValues = "")
        {
            if (JToken.DeepEquals(first, second)) return null;

            if (first != null && second != null && first?.GetType() != second?.GetType())
                throw new InvalidOperationException($"Operands' types must match; '{first.GetType().Name}' <> '{second.GetType().Name}'");

            var propertyNames = (first?.Children() ?? default).Union(second?.Children() ?? default)?.Select(_ => (_ as JProperty)?.Name)?.Distinct();

            if (!propertyNames.Any() && (first is JValue || second is JValue))
            {
                return (first == null) ? second : first;
            }

            var difference = JToken.Parse("{}");

            foreach (var property in propertyNames)
            {
                if (property == null)
                {
                    if (first == null)
                    {
                        difference = second;
                    }
                    // array of object?
                    else if (first is JArray && first.Children().All(c => !(c is JValue)))
                    {
                        var difrences = new JArray();
                        var maximum = Math.Max(first?.Count() ?? 0, second?.Count() ?? 0);

                        for (int i = 0; i < maximum; i++)
                        {
                            var firstsItem = first?.ElementAtOrDefault(i);
                            var secondsItem = second?.ElementAtOrDefault(i);

                            var diff = Differentiate(firstsItem, secondsItem, outputMode, showOriginalValues, replaceValues);

                            if (diff != null)
                            {
                                difrences.Add(diff);
                            }
                        }

                        if (difrences.HasValues)
                        {
                            difference = difrences;
                        }
                    }
                    else
                    {
                        difference = first;
                    }

                    continue;
                }

                if (first?[property] == null)
                {
                    var secondVal = second?[property]?.Parent as JProperty;

                    var targetNode = PointTargetNode(difference, property, ChangeMode.Added, outputMode);

                    if (targetNode.Property != null)
                    {
                        difference[targetNode.Symbol][targetNode.Property] = !string.IsNullOrEmpty(replaceValues) ? replaceValues : secondVal.Value;
                    }
                    else
                        difference[targetNode.Symbol] = !string.IsNullOrEmpty(replaceValues) ? replaceValues : secondVal.Value;

                    continue;
                }

                if (second?[property] == null)
                {
                    var firstVal = first?[property]?.Parent as JProperty;

                    var targetNode = PointTargetNode(difference, property, ChangeMode.Removed, outputMode);

                    if (targetNode.Property != null)
                    {
                        difference[targetNode.Symbol][targetNode.Property] = !string.IsNullOrEmpty(replaceValues) ? replaceValues : firstVal.Value;
                    }
                    else
                        difference[targetNode.Symbol] = !string.IsNullOrEmpty(replaceValues) ? replaceValues : firstVal.Value;

                    continue;
                }

                if (first?[property] is JValue value)
                {
                    if (!JToken.DeepEquals(first?[property], second?[property]))
                    {
                        var targetNode = PointTargetNode(difference, property, ChangeMode.Changed, outputMode);

                        if (targetNode.Property != null)
                        {
                            difference[targetNode.Symbol][targetNode.Property] = !string.IsNullOrEmpty(replaceValues) ? replaceValues : showOriginalValues ? second?[property] : value;
                        }
                        else
                            difference[targetNode.Symbol] = !string.IsNullOrEmpty(replaceValues) ? replaceValues : showOriginalValues ? second?[property] : value;
                        //difference["changed"][property] = showOriginalValues ? second?[property] : value;
                    }

                    continue;
                }

                if (first?[property] is JObject)
                {

                    var targetNode = second?[property] == null
                        ? PointTargetNode(difference, property, ChangeMode.Removed, outputMode)
                        : PointTargetNode(difference, property, ChangeMode.Changed, outputMode);

                    var firstsItem = first[property];
                    var secondsItem = second[property];

                    var difference2 = Differentiate(firstsItem, secondsItem, outputMode, showOriginalValues, replaceValues);

                    if (difference2 != null)
                    {

                        if (targetNode.Property != null)
                        {
                            difference[targetNode.Symbol][targetNode.Property] = difference2;
                        }
                        else
                            difference[targetNode.Symbol] = difference2;

                    }

                    continue;
                }

                if (first?[property] is JArray)
                {
                    var differences = new JArray();

                    var targetNode = second?[property] == null
                       ? PointTargetNode(difference, property, ChangeMode.Removed, outputMode)
                       : PointTargetNode(difference, property, ChangeMode.Changed, outputMode);

                    var maximum = Math.Max(first?[property]?.Count() ?? 0, second?[property]?.Count() ?? 0);

                    for (int i = 0; i < maximum; i++)
                    {
                        var firstsItem = first[property]?.ElementAtOrDefault(i);
                        var secondsItem = second[property]?.ElementAtOrDefault(i);

                        var diff = Differentiate(firstsItem, secondsItem, outputMode, showOriginalValues, replaceValues);

                        if (diff != null)
                        {
                            differences.Add(diff);
                        }
                    }

                    if (differences.HasValues)
                    {
                        if (targetNode.Property != null)
                        {
                            difference[targetNode.Symbol][targetNode.Property] = differences;
                        }
                        else
                            difference[targetNode.Symbol] = differences;
                    }

                    continue;
                }
            }

            return difference;
        }

        public JToken Differentiate(JToken first, JToken second)
        {
            return Differentiate(first, second, this.OutputMode, this.ShowOriginalValues, this.ReplaceValues);
        }
    }
}
