namespace TqTool;

public interface ICommandLineHandler
{
	Task GetHomesAsync();

	Task GetOwnerAsync();

	Task GetPriceAsync(int hours);

	Task GetConsumptionAsync(int days);
}