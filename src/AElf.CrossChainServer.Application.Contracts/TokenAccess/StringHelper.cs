using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AElf.Types;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AElf.CrossChainServer.TokenAccess;

public static class StringHelper
{
    public static decimal SafeToDecimal(this string s, decimal defaultValue = 0)
    {
        return decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public static T ReplaceObjectWithDict<T>(T input, Dictionary<string, string> replacement)
    {
        var json = JsonConvert.SerializeObject(input);
        json = json.ReplaceWithDict(replacement);
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// replace all {param.key} in string
    private static string ReplaceWithDict(this string input, Dictionary<string, string> replacements,
        bool throwErrorIfNotFound = true, string defaultValue = "")
    {
        foreach (var pair in replacements)
        {
            var key = "{" + pair.Key + "}";
            if (input.Contains(key))
            {
                input = input.Replace(key, pair.Value);
            }
            else if (throwErrorIfNotFound)
            {
                throw new Exception($"Key '{key}' not found in the input string.");
            }
            else
            {
                input = input.Replace(key, defaultValue);
            }
        }

        return input;
    }
}