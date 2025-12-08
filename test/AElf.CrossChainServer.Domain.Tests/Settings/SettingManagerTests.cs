using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.Settings;

public class SettingManagerTests: CrossChainServerDomainTestBase
{
    private readonly ISettingManager _settingManager;

    public SettingManagerTests()
    {
        _settingManager = GetRequiredService<ISettingManager>();
    }

    [Fact]
    public async Task SettingTest()
    {
        var chainId = "AELF";
        var key = "key";
        var value = "value";
        var getValue = await _settingManager.GetOrNullAsync(chainId, key);
        getValue.ShouldBeNull();

        await _settingManager.SetAsync(chainId, key, value);
        getValue = await _settingManager.GetOrNullAsync(chainId, key);
        getValue.ShouldBe(value);
    }
}