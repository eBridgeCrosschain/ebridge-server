using System;
using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.TokenPool;

namespace AElf.CrossChainServer;

public class MockIndexerAppService : CrossChainServerAppService, IIndexerAppService
{
    public async Task<long> GetLatestIndexHeightAsync(string chainId)
    {
        return 110;
    }

    public Task<long> GetLatestIndexBestHeightAsync(string chainId)
    {
        throw new System.NotImplementedException();
    }

    public async Task<(bool, CrossChainTransferInfoDto)> GetPendingTransactionAsync(string chainId,
        string transferTransactionId)
    {
        var dto = new CrossChainTransferInfoDto()
        {
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
            TransferTransactionId = "TransferTransactionId",
            TransferBlockHeight = 100,
            TransferTime = DateTime.UtcNow.AddMinutes(-1),
            TransferAmount = 100,
        };
        return (true, dto);
    }

    public async Task<(bool, CrossChainTransferInfoDto)> GetPendingReceiveTransactionAsync(string chainId,
        string transferTransactionId)
    {
        var dto = new CrossChainTransferInfoDto
        {
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
            TransferTransactionId = "TransferTransactionId",
            ReceiveTransactionId = "ReceiveTransactionId",
            TransferBlockHeight = 100,
            ReceiveBlockHeight = 110,
            TransferTime = DateTime.UtcNow.AddMinutes(-1),
            ReceiveTime = DateTime.UtcNow,
            ReceiveAmount = 100
        };
        return (true, dto);
    }

    public async Task<(bool, CrossChainTransferInfoDto)> GetPendingReceiptAsync(string chainId, string receiptId)
    {
        var dto = new CrossChainTransferInfoDto();
        if (chainId == "Ethereum")
        {
            dto = new CrossChainTransferInfoDto
            {
                FromChainId = chainId,
                ToChainId = "MainChain_AELF",
                FromAddress = "FromAddress",
                ToAddress = "ToAddress",
                TransferTransactionId = "TransferTransactionId",
                ReceiveTransactionId = "ReceiveTransactionId",
                TransferBlockHeight = 100,
                ReceiveBlockHeight = 110,
                TransferTime = DateTime.UtcNow.AddMinutes(-1),
                ReceiveTime = DateTime.UtcNow,
                ReceiveAmount = 100,
                ReceiptId = "ReceiptId",
                TransferAmount = 100
            };
        }
        else if ((chainId == "MainChain_AELF"))
        {
            dto = new CrossChainTransferInfoDto
            {
                FromChainId = chainId,
                ToChainId = "Ethereum",
                FromAddress = "FromAddress",
                ToAddress = "ToAddress",
                TransferTransactionId = "TransferTransactionId",
                ReceiveTransactionId = "ReceiveTransactionId",
                TransferBlockHeight = 100,
                ReceiveBlockHeight = 110,
                TransferTime = DateTime.UtcNow.AddMinutes(-1),
                ReceiveTime = DateTime.UtcNow,
                ReceiveAmount = 100,
                ReceiptId = "ReceiptId",
                TransferAmount = 100
            };
        }
        else
        {
            dto = new CrossChainTransferInfoDto
            {
                FromChainId = "Ton",
                ToChainId = "MainChain_AELF",
                FromAddress = "FromAddress",
                ToAddress = "ToAddress",
                TransferTransactionId = "txId",
                ReceiveTransactionId = "ReceiveTransactionId",
                TransferBlockHeight = 100,
                ReceiveBlockHeight = 110,
                TransferTime = DateTime.UtcNow.AddMinutes(-1),
                ReceiveTime = DateTime.UtcNow,
                ReceiveAmount = 100,
                ReceiptId = "ReceiptId",
                TransferAmount = 100
            };
        }

        return (true, dto);
    }
}