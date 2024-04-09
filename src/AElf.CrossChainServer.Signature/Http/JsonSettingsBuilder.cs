using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AElf.CrossChainServer.Signature.Http;

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
