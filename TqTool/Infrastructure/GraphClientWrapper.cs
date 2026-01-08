using GraphQL;
using GraphQL.Client.Abstractions;

namespace TqTool.Infrastructure;

public class GraphClientWrapper(IGraphQLClient client) : IGraphClientWrapper
{
	public async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest query)
	{
		var response = await client.SendQueryAsync<T>(query);
		return response;
	}
}