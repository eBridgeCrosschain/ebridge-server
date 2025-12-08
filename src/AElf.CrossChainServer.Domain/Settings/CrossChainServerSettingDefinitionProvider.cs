using Volo.Abp.Settings;

namespace AElf.CrossChainServer.Settings;

public class CrossChainServerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        context.Add(new SettingDefinition(CrossChainServerSettings.CrossChainTransferIndexerSync));
        context.Add(new SettingDefinition(CrossChainServerSettings.CrossChainIndexingIndexerSync));
        context.Add(new SettingDefinition(CrossChainServerSettings.OracleQueryIndexerSync));
        context.Add(new SettingDefinition(CrossChainServerSettings.ReportIndexerSync));
    }
}
