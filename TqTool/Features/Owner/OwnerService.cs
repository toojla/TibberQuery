using GraphQL;
using Microsoft.Extensions.Logging;
using TqTool.Features.Owner.Models;
using TqTool.Infrastructure;

namespace TqTool.Features.Owner;

public class OwnerService : IOwnerService
{
	private readonly ILogger<OwnerService> _logger;
	private readonly IGraphClientWrapper _graphQlClientWrapper;

	public OwnerService(ILogger<OwnerService> logger,
		IGraphClientWrapper graphQlClientWrapper)
	{
		_logger = logger;
		_graphQlClientWrapper = graphQlClientWrapper;
	}

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

		_logger.LogDebug("Trying to get owner from service!");
		var name = string.Empty;
		var response = await _graphQlClientWrapper.SendQueryAsync<OwnerWrapper>(query);

		if (response.Errors != null && response.Errors.Any())
		{
			foreach (var error in response.Errors)
			{
				_logger.LogError(error.Message);
			}
		}
		else
		{
			_logger.LogDebug("Found owner information...");
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

		_logger.LogDebug("Trying to get owner homes from service!");
		var response = await _graphQlClientWrapper.SendQueryAsync<HomeWrapper>(query);

		if (response.Errors != null && response.Errors.Any())
		{
			foreach (var error in response.Errors)
			{
				_logger.LogError(error.Message);
			}
		}
		else
		{
			_logger.LogDebug("Found owner homes...");
			returnValue.AddRange(response.Data.Viewer.Homes.Select(home => home with
			{
				Address = new Address(home.Address.Address1, home.Address.Address2, home.Address.Address3, home.Address.City, home.Address.PostalCode, home.Address.Country)
			}));
		}

		return returnValue;
	}
}