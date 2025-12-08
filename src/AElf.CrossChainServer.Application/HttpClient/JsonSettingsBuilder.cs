using System;
using AElf.Types;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Utilities.Encoders;

namespace AElf.CrossChainServer.HttpClient;

public class JsonSettingsBuilder
{

    private readonly JsonSerializerSettings _instance = new();

    private JsonSettingsBuilder()
    {
    }

    public static JsonSettingsBuilder New()
    {
        return new JsonSettingsBuilder();
    }
    
    public JsonSerializerSettings Build()
    {
        return _instance;
    }


    public JsonSettingsBuilder WithCamelCasePropertyNamesResolver()
    {
        _instance.ContractResolver = new CamelCasePropertyNamesContractResolver();
        return this;
    }
    
    public JsonSettingsBuilder IgnoreNullValue()
    {
        _instance.NullValueHandling = NullValueHandling.Ignore;
        return this;
    }

}
