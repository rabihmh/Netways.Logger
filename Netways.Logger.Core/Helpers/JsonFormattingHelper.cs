using System.Text.Json;
using System.Text.RegularExpressions;

namespace Netways.Logger.Core.Helpers
{
    public static class JsonFormattingHelper
    {
        /// <summary>
        /// Checks if a given string is valid JSON
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <returns>True if the string is valid JSON, false otherwise</returns>
        public static bool IsJson(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) 
                return false;

            value = value.TrimStart();
            if (!(value.StartsWith("{") || value.StartsWith("["))) 
                return false;

            try
            {
                JsonDocument.Parse(value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to format a JSON string with proper indentation
        /// </summary>
        /// <param name="json">The JSON string to format</param>
        /// <returns>Formatted JSON string or original string if formatting fails</returns>
        public static string TryFormatJson(string json)
        {
            try
            {
                // Remove double serialization
                json = json.Replace("\\\"", "\"").TrimStart('"').TrimEnd('"');

                if (IsJson(json))
                {
                    return FormatJson(json);
                }

                return json;
            }
            catch
            {
                return json; // Return original text if parsing fails
            }
        }

        /// <summary>
        /// Formats a JSON string with proper indentation and encoding
        /// </summary>
        /// <param name="json">The JSON string to format</param>
        /// <returns>Formatted JSON string</returns>
        public static string FormatJson(string json)
        {
            try
            {
                json = Regex.Unescape(json);

                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// Safely converts an object to JSON string
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>JSON string representation of the object</returns>
        public static string SafeSerializeObject(object obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch
            {
                return obj?.ToString() ?? "null";
            }
        }
    }
} 