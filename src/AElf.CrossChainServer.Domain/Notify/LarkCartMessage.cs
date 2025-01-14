using System.Collections.Generic;

namespace AElf.CrossChainServer.Notify;

public class LarkCardMessage : LarkGroupRobotMessageBase
{
    public LarkCardMessage() : base(LarkMessageTypeEnum.Interactive)
    {
        Card = new CardMessage();
    }
    
    public CardMessage Card { get; set; }

    
    public static CardMessageBuilder Builder()
    {
        return new CardMessageBuilder();
    }
    
    // classes
    public class CardMessage
    {
        
        public List<ElementBase> Elements { get; set; }
        public CardHeader Header { get; set; }

    }


    public class CardHeader
    {
        public CardHeader(string title, string template = "default")
        {
            Template = template;
            Title = new TextElement(title);
        }
        
        public string Template { get; set; }
        public ContentElement Title { get; set; }
    }
    
    public abstract class ElementBase
    {
        public string Tag { get; set; }

        public ElementBase(string tag)
        {
            Tag = tag;
        }
    }

    public class ContentElement: ElementBase
    {
        public string Content { get; set; }

        public ContentElement(string tag, string content) : base(tag)
        {
            Content = content;
        }
    }

    public class TextElement : ContentElement
    {
        public TextElement(string content) : base("plain_text", content)
        {
        }
    }

    public class MarkdownElementText : ContentElement
    {
        public MarkdownElementText(string content) : base("markdown", content)
        {
        }
    }
    
}

public class CardMessageBuilder : LarkMessageBuilder
{
    private readonly LarkCardMessage _message = new();

    public LarkCardMessage Build()
    {
        return base.Build(_message);
    }

    public CardMessageBuilder WithSignature(string secret)
    {
        WithSign(secret);
        return this;
    }

    public CardMessageBuilder WithTitle(string title, string template = "default")
    {
        _message.Card.Header = new LarkCardMessage.CardHeader(title, template);
        return this;
    }

    public CardMessageBuilder AddMarkdownContent(string content)
    {
        _message.Card.Elements ??= new();
        _message.Card.Elements.Add(new LarkCardMessage.MarkdownElementText(content));
        return this;
    }
    
    public CardMessageBuilder AddMarkdownContents(List<string> contents)
    {
        _message.Card.Elements ??= new();
        foreach (var content in contents)
        {
            _message.Card.Elements.Add(new LarkCardMessage.MarkdownElementText(content));
        }
        return this;
    }
    
}