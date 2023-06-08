using TqTool.Features.Owner.Models;

namespace TqTool.Features.Owner;

public interface IOwnerService
{
	Task<OwnerResult> GetOwnerAsync();

	Task<IEnumerable<Home>> GetOwnerHomesAsync();
}