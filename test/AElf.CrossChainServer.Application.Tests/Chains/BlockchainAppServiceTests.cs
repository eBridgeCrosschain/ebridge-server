using System.Threading.Tasks;
using Xunit;
using Shouldly;

namespace AElf.CrossChainServer.Chains;

public class BlockchainAppServiceTests: CrossChainServerApplicationTestBase
{
    private readonly IBlockchainAppService _blockchainAppService;

    public BlockchainAppServiceTests()
    {
        _blockchainAppService = GetRequiredService<IBlockchainAppService>();
    }
    
    [Fact]
    public async Task GetChainStatus_Test()
    {
        var chainStatus = await _blockchainAppService.GetChainStatusAsync("MainChain_AELF");
        chainStatus.ChainId.ShouldBe("MainChain_AELF");
        chainStatus.BlockHeight.ShouldBe(100);
        chainStatus.ConfirmedBlockHeight.ShouldBe(90);
    }
    
    [Fact]
    public async Task GetTransactionResult_Test()
    {
        var transactionResult = await _blockchainAppService.GetTransactionResultAsync("MainChain_AELF","txid");
        transactionResult.ChainId.ShouldBe("MainChain_AELF");
    }
    
    [Fact]
    public async Task GetMerklePath_Test()
    {
        var merklePath = await _blockchainAppService.GetMerklePathAsync("MainChain_AELF", "txid");
        merklePath.ShouldNotBeNull();
    }
}