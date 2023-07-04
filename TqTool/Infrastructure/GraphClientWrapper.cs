using GraphQL;
using GraphQL.Client.Abstractions;

namespace TqTool.Infrastructure;

public class GraphClientWrapper : IGraphClientWrapper
{
	private readonly IGraphQLClient _client;

	public GraphClientWrapper(IGraphQLClient client)
	{
		_client = client;
	}

	public async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest query)
	{
		var response = await _client.SendQueryAsync<T>(query);
		return response;
	}
}