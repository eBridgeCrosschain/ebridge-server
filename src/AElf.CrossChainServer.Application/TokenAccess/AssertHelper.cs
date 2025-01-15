using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp;

namespace AElf.CrossChainServer.TokenAccess;

public static class AssertHelper
{
    private const int DefaultErrorCode = 50000;
    private const string DefaultErrorReason = "Assert failed";

    public static void IsTrue(bool expression, [CanBeNull] string reason)
    {
        IsTrue(expression, DefaultErrorCode, reason);
    }

    public static void IsTrue(bool expression, int code = DefaultErrorCode,
        [CanBeNull] string reason = DefaultErrorReason)
    {
        if (!expression)
        {
            throw new UserFriendlyException(
                Format(reason), code.ToString());
        }
    }

    private static string Format(string template, params object[] values)
    {
        if (values == null || values.Length == 0)
            return template;

        var valueIndex = 0;
        var start = 0;
        int placeholderStart;
        var result = new StringBuilder();

        while ((placeholderStart = template.IndexOf('{', start)) != -1)
        {
            var placeholderEnd = template.IndexOf('}', placeholderStart);
            if (placeholderEnd == -1) break;

            result.Append(template, start, placeholderStart - start);

            if (valueIndex < values.Length)
                result.Append(values[valueIndex++] ?? "null");
            else
                result.Append(template, placeholderStart, placeholderEnd - placeholderStart + 1);

            start = placeholderEnd + 1;
        }

        if (start < template.Length)
            result.Append(template, start, template.Length - start);

        return result.ToString();
    }
}