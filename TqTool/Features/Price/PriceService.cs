using GraphQL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TqTool.Features.Price.Models;
using TqTool.Infrastructure;

namespace TqTool.Features.Price;

public class PriceService(
	IGraphClientWrapper graphClientWrapper,
	IMemoryCache memoryCache,
	IPriceViewModelFactory priceViewModelFactory,
	ILogger<PriceService> logger)
	: IPriceService
{
	private const string _cacheKey = "price";

	public async Task<PriceSummaryViewModel> GetPriceAsync(int hours)
	{
		logger.LogDebug("Trying to get prices from cache...");
		memoryCache.TryGetValue(_cacheKey, out PriceResultWrapper? priceResultWrapper);

		if (priceResultWrapper == null)
		{
			logger.LogDebug("No cached prices, trying to get prices from service!");
			var response = await GetPriceFromServiceAsync();
			priceResultWrapper = response.Data;

			if (response.Errors != null && response.Errors.Any())
			{
				foreach (var error in response.Errors)
				{
					logger.LogError("{ApiError}", error.Message);
				}
			}

			if (priceResultWrapper == null)
			{
				throw new InvalidOperationException($"The Tibber api returned no price data: {GraphQlErrors.Describe(response.Errors)}");
			}
		}

		logger.LogDebug("Found prices, trying to format them...");
		var homes = priceResultWrapper.Viewer.Homes;
		var home = homes.FirstOrDefault();

		if (home == null) throw new InvalidOperationException("There is no price info!");
		if (home.CurrentSubscription == null) throw new InvalidOperationException("There is no current subscription!");

		var priceSummaryViewModel = priceViewModelFactory.CreateModel(home.CurrentSubscription.PriceInfo, hours);
		return priceSummaryViewModel;
	}

	private async Task<GraphQLResponse<PriceResultWrapper>> GetPriceFromServiceAsync()
	{
		var query = new GraphQLRequest
		{
			Query = @"{
						viewer {
							homes {
								currentSubscription {
									priceInfo {
										today {
											total
											energy
											tax
											startsAt
											currency
										}
										tomorrow {
											total
											energy
											tax
											startsAt
											currency
										}
									}
								}
							}
						}
					}"
		};

		var result = await graphClientWrapper.SendQueryAsync<PriceResultWrapper>(query);

		if (result.Data != null)
		{
			memoryCache.Set(_cacheKey, result.Data,
				new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)));
		}

		return result;
	}
}