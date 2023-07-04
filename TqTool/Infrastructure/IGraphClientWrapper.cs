using GraphQL;

namespace TqTool.Infrastructure;

public interface IGraphClientWrapper
{
	Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest query);
}