using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AElfIndexer.BlockSync;

public interface IBlockSyncAppService : IApplicationService
{
    Task<BlockSyncMode> GetBlockSyncModeAsync(string chainId, long blockHeight);
}