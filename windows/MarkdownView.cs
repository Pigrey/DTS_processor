using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Tables;
using WpfBlock=System.Windows.Documents.Block;
using WpfInline=System.Windows.Documents.Inline;
namespace Shixiang;
public class MarkdownView:RichTextBox
{
    public static readonly DependencyProperty SourceProperty=DependencyProperty.Register(nameof(Source),typeof(string),typeof(MarkdownView),new PropertyMetadata("",(d,e)=>((MarkdownView)d).Render()));
    public string Source {get=>(string)GetValue(SourceProperty);set=>SetValue(SourceProperty,value);}
    public MarkdownView(){IsReadOnly=true;IsDocumentEnabled=false;IsHitTestVisible=false;BorderThickness=new(0);Background=Brushes.Transparent;Padding=new(3);VerticalScrollBarVisibility=ScrollBarVisibility.Disabled;HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled;Render();}
    static readonly MarkdownPipeline Pipeline=new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    void Render(){var doc=new FlowDocument{PagePadding=new(0),FontFamily=new("Microsoft YaHei UI"),FontSize=13,Foreground=new SolidColorBrush(Color.FromRgb(48,63,84))};try{foreach(var b in Markdown.Parse(Source??"",Pipeline))foreach(var block in Blocks(b))doc.Blocks.Add(block);}catch{doc.Blocks.Add(new Paragraph(new Run(Source??"")));}Document=doc;}
    static IEnumerable<WpfBlock> Blocks(Markdig.Syntax.Block block)
    {
        if(block is HeadingBlock h){var p=Paragraph(h.Inline);p.FontSize=Math.Max(14,25-h.Level*2);p.FontWeight=FontWeights.Bold;yield return p;}
        else if(block is ParagraphBlock p)yield return Paragraph(p.Inline);
        else if(block is CodeBlock code){yield return new Paragraph(new Run(code.Lines.ToString())){FontFamily=new("Consolas"),Background=new SolidColorBrush(Color.FromRgb(237,241,247)),Padding=new(8),Margin=new(0,5,0,5)};}
        else if(block is ListBlock list){var w=new System.Windows.Documents.List{MarkerStyle=list.IsOrdered?TextMarkerStyle.Decimal:TextMarkerStyle.Disc,Margin=new(0,3,0,3),Padding=new(20,0,0,0)};foreach(var child in list.OfType<ListItemBlock>()){var item=new ListItem();foreach(var b in child)foreach(var rendered in Blocks(b))item.Blocks.Add(rendered);w.ListItems.Add(item);}yield return w;}
        else if(block is QuoteBlock quote){var section=new Section{BorderBrush=Brushes.LightSteelBlue,BorderThickness=new(3,0,0,0),Padding=new(10,0,0,0),Margin=new(0,5,0,5)};foreach(var b in quote)foreach(var rendered in Blocks(b))section.Blocks.Add(rendered);yield return section;}
        else if(block is Markdig.Extensions.Tables.Table table){var w=new System.Windows.Documents.Table{CellSpacing=1,Margin=new(0,5,0,5)};var group=new TableRowGroup();foreach(var row in table.OfType<Markdig.Extensions.Tables.TableRow>()){var wr=new System.Windows.Documents.TableRow();foreach(var cell in row.OfType<Markdig.Extensions.Tables.TableCell>()){var wc=new System.Windows.Documents.TableCell{Padding=new(5),BorderBrush=Brushes.LightGray,BorderThickness=new(.5)};foreach(var child in cell)foreach(var rendered in Blocks(child))wc.Blocks.Add(rendered);if(row.IsHeader)wc.FontWeight=FontWeights.Bold;wr.Cells.Add(wc);}group.Rows.Add(wr);}w.RowGroups.Add(group);yield return w;}
        else if(block is ThematicBreakBlock)yield return new Paragraph(new Run("────────────────")){Foreground=Brushes.LightGray};
        else if(block is ContainerBlock container){foreach(var child in container)foreach(var b in Blocks(child))yield return b;}
        else if(block is LeafBlock leaf)yield return new Paragraph(new Run(leaf.Lines.ToString()));
    }
    static Paragraph Paragraph(ContainerInline? inline){var p=new Paragraph{Margin=new(0,2,0,4)};if(inline!=null)foreach(var i in inline)foreach(var w in Inlines(i))p.Inlines.Add(w);return p;}
    static IEnumerable<WpfInline> Inlines(Markdig.Syntax.Inlines.Inline i){
        if(i is LiteralInline literal)yield return new Run(literal.Content.ToString());
        else if(i is CodeInline code)yield return new Run(code.Content){FontFamily=new("Consolas"),Background=new SolidColorBrush(Color.FromRgb(237,241,247))};
        else if(i is LineBreakInline)yield return new LineBreak();
        else if(i is EmphasisInline emphasis){var span=new Span();if(emphasis.DelimiterChar=='~')span.TextDecorations=TextDecorations.Strikethrough;else if(emphasis.DelimiterCount>=2)span.FontWeight=FontWeights.Bold;else span.FontStyle=FontStyles.Italic;foreach(var child in emphasis)foreach(var w in Inlines(child))span.Inlines.Add(w);yield return span;}
        else if(i is LinkInline link){var span=new Span{Foreground=Brushes.RoyalBlue};if(link.IsImage)span.Inlines.Add(new Run("[图片] "));foreach(var child in link)foreach(var w in Inlines(child))span.Inlines.Add(w);if(!string.IsNullOrEmpty(link.Url))span.Inlines.Add(new Run(" ("+link.Url+")"));yield return span;}
        else if(i is HtmlInline html)yield return new Run(html.Tag);
        else if(i is ContainerInline container){foreach(var child in container)foreach(var w in Inlines(child))yield return w;}
        else yield return new Run(i.ToString());
    }
}
