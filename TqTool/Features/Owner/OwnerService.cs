using GraphQL;
using Microsoft.Extensions.Logging;
using TqTool.Features.Owner.Models;
using TqTool.Infrastructure;

namespace TqTool.Features.Owner;

public class OwnerService(
	ILogger<OwnerService> logger,
	IGraphClientWrapper graphQlClientWrapper)
	: IOwnerService
{
	public async Task<OwnerResult> GetOwnerAsync()
	{
		var query = new GraphQLRequest
		{
			Query = @"{
					  viewer {
					    name
					    login
					  }
					}"
		};

		logger.LogDebug("Trying to get owner from service!");
		var name = string.Empty;
		var response = await graphQlClientWrapper.SendQueryAsync<OwnerWrapper>(query);

		if (response.Errors != null && response.Errors.Any())
		{
			foreach (var error in response.Errors)
			{
				logger.LogError("{ApiError}", error.Message);
			}
		}
		else
		{
			logger.LogDebug("Found owner information...");
			name = response.Data.Viewer.Name;
		}

		return new OwnerResult(name);
	}

	public async Task<IEnumerable<Home>> GetOwnerHomesAsync()
	{
		var returnValue = new List<Home>();
		var query = new GraphQLRequest
		{
			Query = @"
			{
				viewer {
					name
					login
					homes {
						size
					    numberOfResidents
						timeZone
						type
					    address {
							address1
					        address2
					        address3
					        postalCode
					        city
					        country
					        latitude
					        longitude
					    }
					}
				}
			}"
		};

		logger.LogDebug("Trying to get owner homes from service!");
		var response = await graphQlClientWrapper.SendQueryAsync<HomeWrapper>(query);

		if (response.Errors != null && response.Errors.Any())
		{
			foreach (var error in response.Errors)
			{
				logger.LogError("{ApiError}", error.Message);
			}
		}
		else
		{
			logger.LogDebug("Found owner homes...");
			returnValue.AddRange(response.Data.Viewer.Homes);
		}

		return returnValue;
	}
}