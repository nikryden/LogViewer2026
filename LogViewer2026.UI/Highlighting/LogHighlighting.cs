using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.IO;
using System.Reflection;
using System.Xml;

namespace LogViewer2026.UI.Highlighting;

public static class LogHighlighting
{
    public static IHighlightingDefinition CreateLogHighlighting()
    {
        var xshd = @"<?xml version='1.0'?>
<SyntaxDefinition name='Log' xmlns='http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008'>
    <Color name='Error' foreground='Red' fontWeight='bold' />
    <Color name='Fatal' foreground='DarkRed' fontWeight='bold' />
    <Color name='Warning' foreground='Orange' />
    <Color name='Information' foreground='Blue' />
    <Color name='Debug' foreground='Gray' />
    <Color name='Verbose' foreground='DarkGray' />
    <Color name='Timestamp' foreground='Green' />
    <Color name='LineNumber' foreground='Purple' />
    
    <RuleSet>
        <!-- Line numbers at start -->
        <Rule color='LineNumber'>
            ^\[\d+\]
        </Rule>
        
        <!-- Timestamps -->
        <Rule color='Timestamp'>
            \d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3}
        </Rule>
        
        <!-- Log Levels -->
        <Rule color='Error'>
            \[Error\]|\[ERR\]
        </Rule>
        
        <Rule color='Fatal'>
            \[Fatal\]|\[FTL\]
        </Rule>
        
        <Rule color='Warning'>
            \[Warning\]|\[WRN\]
        </Rule>
        
        <Rule color='Information'>
            \[Information\]|\[INF\]
        </Rule>
        
        <Rule color='Debug'>
            \[Debug\]|\[DBG\]
        </Rule>
        
        <Rule color='Verbose'>
            \[Verbose\]|\[VRB\]
        </Rule>
    </RuleSet>
</SyntaxDefinition>";

        using var reader = new StringReader(xshd);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
