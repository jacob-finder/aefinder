using AElf.Orleans.EventSourcing.Snapshot;
using AElfScan.Grains.EventData;
using AElfScan.Grains.State.Blocks;
using Microsoft.Extensions.Logging;

namespace AElfScan.Grains.Grain.Blocks;

public class BlockGrain:JournaledSnapshotGrain<BlockState>,IBlockGrain
{
    private readonly ILogger<BlockGrain> _logger;
    
    public BlockGrain(
        ILogger<BlockGrain> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, BlockEventData>> GetBlockDictionary()
    {
        return this.State.Blocks;
    }
    
    public async Task InitializeStateAsync(Dictionary<string, BlockEventData> blocksDictionary)
    {
        foreach (KeyValuePair<string,BlockEventData> keyValueData in blocksDictionary)
        {
            BlockStateEventData blockStateEventData = new BlockStateEventData();
            blockStateEventData.BlockHash = keyValueData.Key;
            blockStateEventData.BlockInfo = keyValueData.Value;
            RaiseEvent(blockStateEventData);
            await ConfirmEvents();
        }
    }

    public async Task<List<BlockEventData>> SaveBlock(BlockEventData blockEvent)
    {
        //Ignore blocks with height less than LIB block in Dictionary
        var dicLibBlock = this.State.Blocks.Where(b => b.Value.IsConfirmed)
            .Select(x => x.Value)
            .FirstOrDefault();
        if (dicLibBlock != null && dicLibBlock.BlockNumber >= blockEvent.BlockNumber)
        {
            // Console.WriteLine($"[BlockGrain]Block {blockEvent.BlockNumber} smaller than dicLibBlock {dicLibBlock.BlockNumber},so ignored");
            return null;
        }

        //Ensure block continuity
        if (this.State.Blocks.Count > 0 && !this.State.Blocks.ContainsKey(blockEvent.PreviousBlockHash))
        {
            // Console.WriteLine(
            //     $"[BlockGrain]Block {blockEvent.BlockNumber} can't be processed now, its PreviousBlockHash is not exist in dictionary");
            throw new Exception(
                $"Block {blockEvent.BlockNumber} can't be processed now, its PreviousBlockHash is not exist in dictionary");
        }


        BlockEventData currentLibBlock =
            this.State.FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);

        List<BlockEventData> libBlockList = new List<BlockEventData>();
        if (currentLibBlock != null)
        {
            GetLibBlockList(currentLibBlock.BlockHash, libBlockList);
        }

        RaiseEvent(blockEvent, blockEvent.LibBlockNumber > 0);
        await ConfirmEvents();

        return libBlockList;
    }

    public async Task<List<BlockEventData>> SaveBlocks(List<BlockEventData> blockEventDataList)
    {
        if (this.State.Blocks.Count > 0)
        {
            // Ignore blocks with height less than LIB block in Dictionary
            var dicLibBlock = this.State.Blocks.Where(b => b.Value.IsConfirmed)
                .Select(x => x.Value)
                .FirstOrDefault();
            if (dicLibBlock != null)
            {
                if (dicLibBlock.BlockNumber >= blockEventDataList.OrderBy(x => x.BlockNumber).Last().BlockNumber)
                {
                    // Console.WriteLine($"[BlockGrain]Block {blockEvent.BlockNumber} smaller than dicLibBlock {dicLibBlock.BlockNumber},so ignored");
                    return null;
                }

                blockEventDataList = blockEventDataList.Where(b =>
                        b.BlockNumber > dicLibBlock.BlockNumber &&
                        !State.Blocks.ContainsKey(b.BlockHash))
                    .ToList();
                
                if (blockEventDataList.Count == 0)
                {
                    return null;
                }
            }
            
            //Ensure block continuity
            if (!this.State.Blocks.ContainsKey(blockEventDataList.First().PreviousBlockHash))
            {
                Console.WriteLine(
                    $"[BlockGrain]Block {blockEventDataList.First().BlockNumber} can't be processed now, its PreviousBlockHash is not exist in dictionary");
                throw new Exception(
                    $"Block {blockEventDataList.First().BlockNumber} can't be processed now, its PreviousBlockHash is not exist in dictionary");
            }
            
        }
        
        
        List<BlockEventData> libBlockList = new List<BlockEventData>();

        if (blockEventDataList.Count == 0)
        {
            return null;
        }

        if (blockEventDataList.Count == 1)
        {
            var blockEvent = blockEventDataList.First();
            BlockEventData currentLibBlock =
                State.FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);
            if (currentLibBlock != null)
            {
                GetLibBlockList(currentLibBlock.BlockHash, libBlockList);
            }

            blockEvent.ClearBlockStateDictionary = true;
            RaiseEvent(blockEvent, blockEvent.LibBlockNumber > 0);
            await ConfirmEvents();
        }
        else
        {
            long maxLibBlockNumber = blockEventDataList.Max(b => b.LibBlockNumber);

            List<BlockEventData> eventList = new List<BlockEventData>();
            if (maxLibBlockNumber > 0)
            {
                //find the last block index in list with block's lib block number is maxLibBlockNumber
                var index = blockEventDataList.FindLastIndex(b => b.LibBlockNumber == maxLibBlockNumber);

                var beforeList = blockEventDataList.GetRange(0, index);
                RaiseEvents(beforeList);
                await ConfirmEvents();

                var blockEvent = blockEventDataList[index];
                var currentLibBlock = State.FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);

                if (currentLibBlock != null)
                {
                    GetLibBlockList(currentLibBlock.BlockHash, libBlockList);
                }

                blockEvent.ClearBlockStateDictionary = true;
                RaiseEvent(blockEvent, blockEvent.LibBlockNumber > 0);
                await ConfirmEvents();

                var afterList = blockEventDataList.GetRange(index + 1, blockEventDataList.Count - index - 1);
                RaiseEvents(afterList);
                await ConfirmEvents();
            }
            else
            {
                RaiseEvents(blockEventDataList);
                await ConfirmEvents();
            }

        }

        return libBlockList;

    }

    private void GetLibBlockList(string currentLibBlockHash, List<BlockEventData> libBlockList)
    {
        while (this.State.Blocks.ContainsKey(currentLibBlockHash))
        {
            if (this.State.Blocks[currentLibBlockHash].IsConfirmed)
            {
                libBlockList.Add(this.State.Blocks[currentLibBlockHash]);
                return;
            }
            
            this.State.Blocks[currentLibBlockHash].IsConfirmed = true;
            libBlockList.Add(this.State.Blocks[currentLibBlockHash]);
            currentLibBlockHash = this.State.Blocks[currentLibBlockHash].PreviousBlockHash;
        }
    }
    
}