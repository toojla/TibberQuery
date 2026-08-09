using GraphQL;
using Microsoft.Extensions.Logging;
using TqTool.Features.Consumption.Models;
using TqTool.Infrastructure;

namespace TqTool.Features.Consumption;

public class ConsumptionService(
	IGraphClientWrapper graphClientWrapper,
	IConsumptionViewModelFactory consumptionViewModelFactory,
	ILogger<ConsumptionService> logger)
	: IConsumptionService
{
	public async Task<ConsumptionViewModel> GetConsumptionAsync(int noOfDays)
	{
		logger.LogDebug("Trying to get consumption from service...");
		var response = await GetPriceFromServiceAsync(noOfDays);

		if (response.Errors != null && response.Errors.Any())
		{
			foreach (var error in response.Errors)
			{
				logger.LogError(error.Message);
			}
		}

		logger.LogDebug($"Searching consumption info for the last {noOfDays} days!");

		if (response.Data == null)
		{
			throw new InvalidOperationException($"The Tibber api returned no consumption data: {GraphQlErrors.Describe(response.Errors)}");
		}

		var consumptionResult = response.Data.Viewer.Homes.FirstOrDefault();

		if (consumptionResult == null) throw new NullReferenceException("There is no consumption info!");

		var consumptionViewModel = consumptionViewModelFactory.CreateModel(consumptionResult.Consumption.Nodes);
		return consumptionViewModel;
	}

	private async Task<GraphQLResponse<ConsumptionWrapper>> GetPriceFromServiceAsync(int noOfDays)
	{
		var query = new GraphQLRequest
		{
			Query = @"query Consumption($days: Int) {
						viewer {
							homes {
								consumption(resolution: DAILY, last: $days) {
									nodes {
										from
								        to
								        cost
								        unitPrice
								        unitPriceVAT
								        consumption
								        consumptionUnit
									}
								}
							}
						}
					}",
			Variables = new { days = noOfDays }
		};

		var result = await graphClientWrapper.SendQueryAsync<ConsumptionWrapper>(query);

		return result;
	}
}