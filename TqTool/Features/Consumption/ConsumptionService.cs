﻿using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using TqTool.Features.Consumption.Models;

namespace TqTool.Features.Consumption;

public class ConsumptionService : IConsumptionService
{
	private readonly IGraphQLClient _client;
	private readonly IConsumptionViewModelFactory _consumptionViewModelFactory;
	private readonly ILogger<ConsumptionService> _logger;

	public ConsumptionService(IGraphQLClient client,
		IConsumptionViewModelFactory consumptionViewModelFactory,
		ILogger<ConsumptionService> logger)
	{
		_client = client;
		_consumptionViewModelFactory = consumptionViewModelFactory;
		_logger = logger;
	}

	public async Task<ConsumptionViewModel> GetConsumptionAsync(int noOfDays)
	{
		_logger.LogDebug("Trying to get consumption from service...");
		var response = await GetPriceFromServiceAsync(noOfDays);

		if (response.Errors != null && response.Errors.Any())
		{
			foreach (var error in response.Errors)
			{
				_logger.LogError(error.Message);
			}
		}

		_logger.LogDebug($"Searching consumption info for the last {noOfDays} days!");

		var consumptionResult = response.Data.Viewer.Homes.FirstOrDefault();

		if (consumptionResult == null) throw new NullReferenceException("There is no consumption info!");

		var consumptionViewModel = _consumptionViewModelFactory.CreateModel(consumptionResult.Consumption.Nodes);
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

		var result = await _client.SendQueryAsync<ConsumptionWrapper>(query);

		return result;
	}
}