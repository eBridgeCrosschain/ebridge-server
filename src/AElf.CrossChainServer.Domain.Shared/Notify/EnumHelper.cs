using System;
using System.ComponentModel;

namespace AElf.CrossChainServer.Notify;

public class EnumHelper
{
    public static string GetDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
        
        return attribute == null ? value.ToString() : attribute.Description;
    }
}