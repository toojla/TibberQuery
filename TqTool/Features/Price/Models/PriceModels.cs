namespace TqTool.Features.Price.Models;

public record PriceResultWrapper(PriceResult Viewer);

public record PriceResult(IEnumerable<HomeWrapper> Homes);

public record HomeWrapper(CurrentSubscriptionWrapper CurrentSubscription);

public record CurrentSubscriptionWrapper(PriceInfo PriceInfo);

public record PriceInfo(IEnumerable<EnergyPrice> Today, IEnumerable<EnergyPrice> Tomorrow);

// StartsAt keeps the offset the api sent (the home's own local time) rather than being flattened
// to whatever zone this machine happens to run in.
public record EnergyPrice(decimal Total, decimal Energy, decimal Tax, DateTimeOffset StartsAt, string Currency);

public record PriceSummaryViewModel(PriceViewModel CurrentPrice, IEnumerable<PriceViewModel> UpcomingPrices);

public record PriceViewModel(int Price, int Tax, DateTimeOffset StartsAt);