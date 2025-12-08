using System.Collections.Concurrent;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.Indexer
{
    public class GraphQLClientFactory : IGraphQLClientFactory
    {
        private readonly GraphQLClientOptions _graphQlClientOptions;
        private readonly ConcurrentDictionary<string, IGraphQLClient> _clientDic;

        public GraphQLClientFactory(IOptionsSnapshot<GraphQLClientOptions> graphQlClientOptions)
        {
            _graphQlClientOptions = graphQlClientOptions.Value;
            _clientDic = new ConcurrentDictionary<string, IGraphQLClient>();
        }

        public IGraphQLClient GetClient(GraphQLClientEnum clientEnum)
        {
            var clientName = clientEnum.ToString();
            if (_clientDic.TryGetValue(clientName, out var client))
            {
                return client;
            }

            client = new GraphQLHttpClient(_graphQlClientOptions.Mapping[clientName],
                new NewtonsoftJsonSerializer());
            _clientDic[clientName] = client;
            return client;
        }
    }
}