using AElfIndexer.Grains.Grain.Blocks;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Providers;

public interface IBlockGrainProvider
{
    // Task<IBlockGrain> GetBlockGrain(string chainId);

    Task<IBlockGrain> GetBlockGrain(string chainId, string blockHash);

    Task<bool> GrainExist(string chainId,string blockHash);
}

public class BlockGrainProvider : IBlockGrainProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public BlockGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    // public async Task<IBlockGrain> GetBlockGrain(string chainId)
    // {
    //     var primaryKeyGrain = _clusterClient.GetGrain<IPrimaryKeyGrain>(chainId + AElfIndexerConsts.PrimaryKeyGrainIdSuffix);
    //     var currentPrimaryKey = await primaryKeyGrain.GetCurrentGrainPrimaryKey(chainId);
    //     var primaryKey = await primaryKeyGrain.GetGrainPrimaryKey(chainId);
    //     
    //     if (currentPrimaryKey == primaryKey)
    //     {
    //         return _clusterClient.GetGrain<IBlockGrain>(currentPrimaryKey);
    //     }
    //
    //     var oldGrain = _clusterClient.GetGrain<IBlockGrain>(currentPrimaryKey);
    //     var blocksDictionary =  await oldGrain.GetBlockDictionary();
    //     
    //     var newGrain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);
    //     await newGrain.InitializeStateAsync(blocksDictionary);
    //     
    //     return newGrain;
    // }

    public async Task<IBlockGrain> GetBlockGrain(string chainId, string blockHash)
    {
        string primaryKey = chainId + AElfIndexerConsts.BlockGrainIdSuffix + blockHash;
        var newGrain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);

        return newGrain;
    }

    public async Task<bool> GrainExist(string chainId, string blockHash)
    {
        string primaryKey = chainId + AElfIndexerConsts.BlockGrainIdSuffix + blockHash;
        var grain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);

        var blockHeight = await grain.GetBlockHeight();

        return blockHeight > 0 ? true : false;
    }
}