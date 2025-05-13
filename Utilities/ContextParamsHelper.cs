using System;
using System.Collections.Generic;

namespace Signal2025AzureConversationRelay.Utilities
{
    public static class ContextParamsHelper
    {
        public static string GetParamFromContext(IDictionary<object, object> items, string paramName)
        {
            if (items.TryGetValue(paramName, out var paramObj) && paramObj is string paramString)
            {
                return paramString;
            }
            throw new ArgumentException($"{paramName} not found in FunctionContext.Items");
        }
    }
}
