using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace Shixiang;
public partial class MainWindow
{
    bool filtersVisible;
    readonly TextBlock allValue=new(),activeValue=new(),doneValue=new(),pausedValue=new(),pageTitle=new();
    readonly Dictionary<string,Button> navigation=[];
    readonly Border emptyState=new();
    readonly TextBlock emptyTitle=new(),emptyDescription=new();
    readonly Button filterToggle=new();
    Button? detailAction,deleteAction;
    static Brush ColorBrush(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    static TextBlock Label(string text,double size=12,string color="#8F99AD")=>new(){Text=text,FontSize=size,Foreground=ColorBrush(color)};
    static Border Rounded(UIElement child,string color="#FFFFFF",double radius=14)=>new(){Child=child,Background=ColorBrush(color),CornerRadius=new(radius)};
    void BuildLayout()
    {
        Title="事项簿 · 让每件事都有着落";Width=1500;Height=920;MinWidth=1080;MinHeight=720;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=new("Microsoft YaHei UI");FontSize=13;Foreground=Ink;Background=ColorBrush("#F5F6FA");UseLayoutRounding=true;
        var shell=new Grid();shell.ColumnDefinitions.Add(new(){Width=new(204)});shell.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});Content=shell;
        shell.Children.Add(BuildSidebar());
        var main=new DockPanel{Margin=new(32,24,32,22)};Grid.SetColumn(main,1);shell.Children.Add(main);
        var top=new DockPanel{Margin=new(0,0,0,32)};DockPanel.SetDock(top,Dock.Top);main.Children.Add(top);
        var avatar=Rounded(new TextBlock{Text="淞",Foreground=ColorBrush("#8170C4"),FontSize=13,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center},"#EDE9FA",18);avatar.Width=36;avatar.Height=36;DockPanel.SetDock(avatar,Dock.Right);top.Children.Add(avatar);
        var breadcrumb=Label("个人工作空间     /     事项管理",11);breadcrumb.VerticalAlignment=VerticalAlignment.Center;top.Children.Add(breadcrumb);
        var titleRow=new DockPanel{Margin=new(0,0,0,26)};DockPanel.SetDock(titleRow,Dock.Top);main.Children.Add(titleRow);
        var newButton=Button("＋  新增事项",AddTask);newButton.SetResourceReference(StyleProperty,"PrimaryButton");newButton.VerticalAlignment=VerticalAlignment.Center;DockPanel.SetDock(newButton,Dock.Right);titleRow.Children.Add(newButton);
        var titleStack=new StackPanel();titleRow.Children.Add(titleStack);pageTitle.Text="全部事项";pageTitle.FontSize=28;pageTitle.FontWeight=FontWeights.SemiBold;pageTitle.Foreground=ColorBrush("#28334B");titleStack.Children.Add(pageTitle);
        var intro=Label("把事情理清楚，把时间留给重要的事。",12);intro.Margin=new(0,10,0,0);titleStack.Children.Add(intro);
        var cards=new UniformGrid{Columns=4,Margin=new(-6,0,-6,26)};DockPanel.SetDock(cards,Dock.Top);main.Children.Add(cards);
        cards.Children.Add(StatCard("全部事项","▤",allValue,"#7564CB","#F0ECFC",""));cards.Children.Add(StatCard("进行中","◷",activeValue,"#6D8FC8","#EDF3FC","进行中"));cards.Children.Add(StatCard("已完成","✓",doneValue,"#65A58B","#ECF7F1","已完成"));cards.Children.Add(StatCard("挂起","Ⅱ",pausedValue,"#CAA468","#FCF5E9","挂起"));
        var bottom=new DockPanel{Margin=new(2,14,2,0)};DockPanel.SetDock(bottom,Dock.Bottom);main.Children.Add(bottom);
        var local=Label("●  本地文档存储",10,"#8EA397");DockPanel.SetDock(local,Dock.Right);bottom.Children.Add(local);bottom.Children.Add(Label("单击单元格编辑  ·  双击事项查看详情",10));
        var panel=new DockPanel();var card=Rounded(panel);card.BorderBrush=ColorBrush("#E8EBF2");card.BorderThickness=new(1);main.Children.Add(card);
        var toolbar=new DockPanel{Margin=new(18,16,18,12)};DockPanel.SetDock(toolbar,Dock.Top);panel.Children.Add(toolbar);
        var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Right);toolbar.Children.Add(actions);
        filterToggle.Content="☷  筛选";filterToggle.Click+=(s,e)=>{grid.CommitEdit(DataGridEditingUnit.Cell,true);grid.CommitEdit(DataGridEditingUnit.Row,true);filtersVisible=!filtersVisible;BuildColumns();UpdateDashboard();};actions.Children.Add(filterToggle);
        actions.Children.Add(Button("显示列",ShowColumns));actions.Children.Add(Button("＋ 字段",AddField));actions.Children.Add(Button("↓  导出 Excel",Export));
        var viewName=Label("▦  表格视图",12,"#7B6AC5");viewName.FontWeight=FontWeights.SemiBold;viewName.VerticalAlignment=VerticalAlignment.Center;toolbar.Children.Add(viewName);
        var tools=new DockPanel{Margin=new(20,0,20,14)};DockPanel.SetDock(tools,Dock.Top);panel.Children.Add(tools);
        search.Width=205;search.Tag="⌕  搜索事项、参与人…";search.ToolTip="搜索所有字段";search.Margin=new(0,0,12,0);search.TextChanged+=(s,e)=>{query=search.Text;Refresh();};DockPanel.SetDock(search,Dock.Left);tools.Children.Add(search);
        var mode=new ComboBox{Width=160,Margin=new(0,0,6,0),ItemsSource=new[]{"进展 · 全部展开","进展 · 选中展开","进展 · 全部折叠"},SelectedIndex=data.Mode=="all"?0:data.Mode=="collapsed"?2:1};mode.SelectionChanged+=(s,e)=>{data.Mode=mode.SelectedIndex==0?"all":mode.SelectedIndex==2?"collapsed":"selected";PropertyChanged?.Invoke(this,new(nameof(ProgressMode)));ScheduleSave();};DockPanel.SetDock(mode,Dock.Right);tools.Children.Add(mode);
        var selectedActions=new StackPanel{Orientation=Orientation.Horizontal};tools.Children.Add(selectedActions);
        detailAction=Button("查看详情",()=>OpenDetail(grid.SelectedItem as TaskItem));selectedActions.Children.Add(detailAction);
        deleteAction=Button("删除事项",()=>DeleteTask(grid.SelectedItem as TaskItem,this));deleteAction.SetResourceReference(StyleProperty,"DangerButton");selectedActions.Children.Add(deleteAction);
        var caption=new DockPanel{Margin=new(22,0,20,12)};DockPanel.SetDock(caption,Dock.Top);panel.Children.Add(caption);
        var clear=Button("清除筛选",()=>{filters.Clear();query="";search.Text="";BuildColumns();Refresh();});clear.Padding=new(6,3,6,3);clear.BorderThickness=new(0);clear.Foreground=ColorBrush("#9E92CD");DockPanel.SetDock(clear,Dock.Right);caption.Children.Add(clear);count.FontSize=11;count.Foreground=ColorBrush("#A0A9BA");count.VerticalAlignment=VerticalAlignment.Center;caption.Children.Add(count);
        var footer=new Border{Background=ColorBrush("#FCFCFE"),BorderBrush=ColorBrush("#EEF0F6"),BorderThickness=new(0,1,0,0),Padding=new(20,13,20,13)};DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
        var foot=new DockPanel();footer.Child=foot;saveState.Text="✓ 已从本地文档加载";saveState.FontSize=10;saveState.Foreground=ColorBrush("#83A38F");saveState.VerticalAlignment=VerticalAlignment.Center;DockPanel.SetDock(saveState,Dock.Right);foot.Children.Add(saveState);
        foot.Children.Add(new TextBlock{Text=store.FilePath,ToolTip=store.FilePath,Foreground=ColorBrush("#A3ACBC"),FontSize=10,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new(0,0,16,0),VerticalAlignment=VerticalAlignment.Center});
        grid.ItemsSource=view;grid.AutoGenerateColumns=false;grid.CanUserAddRows=false;grid.CanUserDeleteRows=false;grid.CanUserReorderColumns=true;grid.SelectionMode=DataGridSelectionMode.Single;grid.SelectionUnit=DataGridSelectionUnit.FullRow;grid.HeadersVisibility=DataGridHeadersVisibility.Column;grid.GridLinesVisibility=DataGridGridLinesVisibility.Horizontal;grid.HorizontalGridLinesBrush=ColorBrush("#F0F2F7");grid.BorderThickness=new(0);grid.Background=Brushes.White;grid.RowBackground=Brushes.White;grid.AlternatingRowBackground=Brushes.White;grid.MinRowHeight=60;grid.EnableRowVirtualization=true;grid.IsReadOnly=false;grid.Foreground=ColorBrush("#526078");grid.FontSize=12;grid.SelectionChanged+=(s,e)=>UpdateDashboard();
        grid.MouseDoubleClick+=(s,e)=>{if(FindParent<TextBox>(e.OriginalSource as DependencyObject)!=null||FindParent<ComboBox>(e.OriginalSource as DependencyObject)!=null||FindParent<DatePicker>(e.OriginalSource as DependencyObject)!=null)return;if(FindParent<DataGridRow>(e.OriginalSource as DependencyObject)!=null){grid.CancelEdit();OpenDetail(grid.SelectedItem as TaskItem);e.Handled=true;}};
        grid.BeginningEdit+=(s,e)=>{if(e.Column.SortMemberPath=="Progress"&&data.Mode=="collapsed"){e.Cancel=true;OpenDetail((TaskItem)e.Row.Item);}};
        var tableArea=new Grid();panel.Children.Add(tableArea);tableArea.Children.Add(grid);
        var empty=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};empty.Children.Add(new TextBlock{Text="▤",FontSize=46,Foreground=ColorBrush("#C5BCE8"),HorizontalAlignment=HorizontalAlignment.Center,Margin=new(0,0,0,12)});emptyTitle.FontSize=18;emptyTitle.Foreground=ColorBrush("#6F6A89");emptyTitle.HorizontalAlignment=HorizontalAlignment.Center;empty.Children.Add(emptyTitle);emptyDescription.Margin=new(0,10,0,20);emptyDescription.Foreground=ColorBrush("#A0A7B9");emptyDescription.FontSize=12;empty.Children.Add(emptyDescription);var emptyAdd=Button("＋  新增一条事项",AddTask);emptyAdd.HorizontalAlignment=HorizontalAlignment.Center;emptyAdd.SetResourceReference(StyleProperty,"PrimaryButton");empty.Children.Add(emptyAdd);emptyState.Child=empty;emptyState.Margin=new(0,48,0,0);emptyState.VerticalAlignment=VerticalAlignment.Stretch;tableArea.Children.Add(emptyState);
    }
    UIElement BuildSidebar()
    {
        var dock=new DockPanel{Margin=new(20,30,20,24)};
        var logo=new StackPanel{Orientation=Orientation.Horizontal,Margin=new(0,0,0,40)};DockPanel.SetDock(logo,Dock.Top);dock.Children.Add(logo);var mark=new Image{Source=new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/App.png")),Width=38,Height=38};logo.Children.Add(mark);logo.Children.Add(new TextBlock{Text="事项簿",FontSize=21,FontWeight=FontWeights.SemiBold,Foreground=ColorBrush("#ECEAF3"),Margin=new(10,0,0,0),VerticalAlignment=VerticalAlignment.Center});
        var note=new StackPanel();DockPanel.SetDock(note,Dock.Bottom);dock.Children.Add(note);note.Children.Add(Label("●  我的本地空间",11,"#929CAB"));var line=Label("简单记录，从容推进。",10,"#677184");line.Margin=new(0,10,0,0);note.Children.Add(line);var version=Label("SHIXIANG   /   2.1.1",9,"#525D70");version.Margin=new(0,24,0,0);note.Children.Add(version);
        var nav=new StackPanel();dock.Children.Add(nav);var heading=Label("工作空间",10,"#697286");heading.Margin=new(12,0,0,14);nav.Children.Add(heading);
        foreach(var (key,label,glyph) in new[]{("","全部事项","▦"),("进行中","进行中","◷"),("已完成","已完成","✓"),("挂起","挂起","Ⅱ")}){
            var b=Button("",()=>SelectStatus(key));b.SetResourceReference(StyleProperty,"NavButton");var contents=new DockPanel{Width=128};contents.Children.Add(new TextBlock{Text=glyph,Width=26,FontSize=15});contents.Children.Add(new TextBlock{Text=label,FontSize=12,VerticalAlignment=VerticalAlignment.Center});b.Content=contents;navigation[key]=b;nav.Children.Add(b);
        }
        return new Border{Background=ColorBrush("#252D3D"),Child=dock};
    }
    UIElement StatCard(string name,string glyph,TextBlock number,string color,string tint,string status)
    {
        var contents=new DockPanel{Margin=new(20,18,20,18)};var icon=Rounded(new TextBlock{Text=glyph,FontSize=24,Foreground=ColorBrush(color),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center},tint,12);icon.Width=44;icon.Height=44;icon.Margin=new(0,0,16,0);DockPanel.SetDock(icon,Dock.Right);contents.Children.Add(icon);
        var stack=new StackPanel();stack.Children.Add(Label(name,11));number.FontSize=28;number.FontWeight=FontWeights.SemiBold;number.Foreground=ColorBrush("#384159");number.Margin=new(0,7,0,0);stack.Children.Add(number);contents.Children.Add(stack);
        var card=Rounded(contents);card.Margin=new(6,0,6,0);card.BorderBrush=ColorBrush("#EBEDF3");card.BorderThickness=new(1);card.Cursor=System.Windows.Input.Cursors.Hand;card.ToolTip="查看"+name;card.MouseLeftButtonUp+=(s,e)=>SelectStatus(status);return card;
    }
    void SelectStatus(string status){if(status=="")filters.Remove("status");else filters["status"]=new(status);BuildColumns();Refresh();}
    void UpdateDashboard()
    {
        allValue.Text=tasks.Count.ToString();activeValue.Text=tasks.Count(t=>t.Status=="进行中").ToString();doneValue.Text=tasks.Count(t=>t.Status=="已完成").ToString();pausedValue.Text=tasks.Count(t=>t.Status=="挂起").ToString();
        var selected=filters.TryGetValue("status",out var status)?status.Text:"";foreach(var pair in navigation)pair.Value.Tag=pair.Key==selected?"active":null;pageTitle.Text=selected==""?"全部事项":selected+"事项";
        int filterCount=filters.Count(p=>p.Value.Text!=""||p.Value.From!=null||p.Value.To!=null);filterToggle.Content="☷  筛选"+(filterCount>0?$"  {filterCount}":"");filterToggle.Foreground=filtersVisible?ColorBrush("#7964C9"):ColorBrush("#657087");
        emptyState.Visibility=view.Cast<TaskItem>().Any()?Visibility.Collapsed:Visibility.Visible;emptyTitle.Text=tasks.Count==0?"把想做的事，记在这里":"没有匹配的事项";emptyDescription.Text=tasks.Count==0?"从第一条事项开始，让每一步进展清晰可见。":"试试调整筛选条件，或创建一条新事项。";
        if(detailAction!=null)detailAction.IsEnabled=grid.SelectedItem is TaskItem;if(deleteAction!=null)deleteAction.IsEnabled=grid.SelectedItem is TaskItem;
    }
    bool DeleteTask(TaskItem? task,Window owner)
    {
        if(task==null)return false;
        if(!grid.CommitEdit(DataGridEditingUnit.Cell,true)||!grid.CommitEdit(DataGridEditingUnit.Row,true))return false;
        if(MessageBox.Show(owner,$"确定删除「{(string.IsNullOrWhiteSpace(task.Title)?"未命名事项":task.Title)}」？\n此操作无法撤销。","删除事项",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return false;
        tasks.Remove(task);task.PropertyChanged-=TaskChanged;ScheduleSave();Refresh();return true;
    }
}
