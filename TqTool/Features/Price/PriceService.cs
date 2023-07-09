using GraphQL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TqTool.Features.Price.Models;
using TqTool.Infrastructure;

namespace TqTool.Features.Price;

public class PriceService : IPriceService
{
	private readonly IGraphClientWrapper _graphClientWrapper;
	private readonly IMemoryCache _memoryCache;
	private readonly IPriceViewModelFactory _priceViewModelFactory;
	private readonly ILogger<PriceService> _logger;
	private const string _cacheKey = "price";

	public PriceService(IGraphClientWrapper graphClientWrapper,
		IMemoryCache memoryCache,
		IPriceViewModelFactory priceViewModelFactory,
		ILogger<PriceService> logger)
	{
		_graphClientWrapper = graphClientWrapper;
		_memoryCache = memoryCache;
		_priceViewModelFactory = priceViewModelFactory;
		_logger = logger;
	}

	public async Task<PriceSummaryViewModel> GetPriceAsync(int hours)
	{
		_logger.LogDebug("Trying to get prices from cache...");
		_memoryCache.TryGetValue(_cacheKey, out PriceResultWrapper? priceResultWrapper);

		if (priceResultWrapper == null)
		{
			_logger.LogDebug("No cached prices, trying to get prices from service!");
			var response = await GetPriceFromServiceAsync();
			priceResultWrapper = response.Data;

			if (response.Errors != null && response.Errors.Any())
			{
				foreach (var error in response.Errors)
				{
					_logger.LogError(error.Message);
				}
			}
		}

		_logger.LogDebug("Found prices, trying to format them...");
		var homes = priceResultWrapper.Viewer.Homes;
		var home = homes.FirstOrDefault();

		if (home == null) throw new NullReferenceException("There is no price info!");

		var priceSummaryViewModel = _priceViewModelFactory.CreateModel(home.CurrentSubscription.PriceInfo, hours);
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

		var result = await _graphClientWrapper.SendQueryAsync<PriceResultWrapper>(query);

		if (result.Data != null)
		{
			_memoryCache.Set(_cacheKey, result.Data,
				new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)));
		}

		return result;
	}
}