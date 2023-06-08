namespace TqTool.Features.Owner.Models;

public record OwnerModel(string Name, string Login, IEnumerable<Home> Homes);

public record OwnerWrapper(OwnerModel Viewer);

public record OwnerResult(string Name);

public record Address(string Address1, string Address2, string Address3, string City, int PostalCode, string Country);

public record Home(int Size, int NumberOfResidents, string TimeZone, string Type, Address Address);

public record HomeWrapper(OwnerModel Viewer);