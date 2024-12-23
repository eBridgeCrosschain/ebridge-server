using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class LarkNotifyTemplateOptions
{
    public Dictionary<string, NotifyTemplate> Templates { get; set; }
}

public class NotifyTemplate
{
    public LarkGroupMessageTemplate LarkGroup { get; set; }
}

public class LarkGroupMessageTemplate
{
    public string WebhookUrl { get; set; }
    public string Secret { get; set; }
    public string TitleTemplate { get; set; }
    public string Title { get; set; }
    public List<string> Contents { get; set; }
}