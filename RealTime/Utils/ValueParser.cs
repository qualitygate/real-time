using System;
using System.Text.Json;

namespace QualityGate.RealTime.Utils
{
    /// <summary>
    ///     Contains logic that parses values coming from HTTP requests bodies (JSON).
    /// </summary>
    public static class ValueParser
    {
        /// <summary>
        ///     Converts the given value to most RQL friendly type.
        /// </summary>
        /// <param name="value">A value that was deserialized from a field in a JSON body.</param>
        /// <returns>
        ///     The given value correctly converted to the most friendly RQL type. It follows the below rules:
        ///     - If the value is a JSON element, and it's a string
        /// </returns>
        public static object? ParseValue(this object value)
        {
            return value switch
            {
                JsonElement { ValueKind: JsonValueKind.String } jsonString => jsonString.GetString(),
                JsonElement { ValueKind: JsonValueKind.Number } jsonNumber => ParseNumber(jsonNumber),
                string stringValue => stringValue,
                _ => value
            };
        }

        private static object ParseNumber(JsonElement jsonElement)
        {
            try
            {
                return jsonElement.GetInt64();
            }
            catch (FormatException)
            {
            }

            return jsonElement.GetDouble();
        }
    }
}