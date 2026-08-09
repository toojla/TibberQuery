using GraphQL;

namespace TqTool.Infrastructure;

public static class GraphQlErrors
{
	public static string Describe(GraphQLError[]? errors)
	{
		if (errors == null || !errors.Any())
		{
			return "no data and no errors were returned";
		}

		return string.Join("; ", errors.Select(error => error.Message));
	}
}
