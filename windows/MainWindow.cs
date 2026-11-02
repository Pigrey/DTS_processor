using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
namespace Shixiang;
public class MainWindow:Window,INotifyPropertyChanged
{
    readonly FileStore store;
    readonly AppData data;
    readonly ObservableCollection<TaskItem> tasks;
    readonly ICollectionView view;
    readonly DataGrid grid=new();
    readonly Dictionary<string,Filter> filters=[];
    readonly DispatcherTimer saveTimer=new(){Interval=TimeSpan.FromMilliseconds(350)};
    readonly TextBlock summary=new(),saveState=new(),count=new();
    readonly TextBox search=new(){Width=190,VerticalContentAlignment=VerticalAlignment.Center,Margin=new(5)};
    string query="";bool dirty;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string ProgressMode=>data.Mode;
    static readonly Brush Ink=new SolidColorBrush(Color.FromRgb(37,55,80)),Blue=new SolidColorBrush(Color.FromRgb(52,120,245));
    static readonly string[] Statuses=["进行中","已完成","挂起"];
    public MainWindow(FileStore store){
        this.store=store;data=store.State.Data;tasks=new(data.Tasks);view=CollectionViewSource.GetDefaultView(tasks);view.Filter=o=>TaskFilter.Matches((TaskItem)o,data.AllFields,filters,query);
        Title="事项簿 · 本地事项跟踪";Width=1460;Height=900;MinWidth=960;MinHeight=620;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=new("Microsoft YaHei UI");FontSize=13;Foreground=Ink;Background=new SolidColorBrush(Color.FromRgb(246,248,252));
        foreach(var t in tasks)t.PropertyChanged+=TaskChanged;
        var root=new DockPanel{Margin=new(28)};Content=root;
        var heading=new StackPanel{Margin=new(0,0,0,20)};DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
        heading.Children.Add(new TextBlock{Text="SHIXIANG  /  个人工作空间",FontSize=11,Foreground=Brushes.SlateGray,Margin=new(0,0,0,10)});
        heading.Children.Add(new TextBlock{Text="全部事项",FontSize=29,FontWeight=FontWeights.SemiBold});
        heading.Children.Add(new TextBlock{Text="记录进展，专注当下。每一件事，都有着落。",Foreground=Brushes.SlateGray,Margin=new(0,8,0,14)});
        summary.FontSize=16;heading.Children.Add(summary);
        var toolbar=new WrapPanel{Margin=new(0,0,0,14)};DockPanel.SetDock(toolbar,Dock.Top);root.Children.Add(toolbar);
        var add=Button("＋ 新增事项",AddTask);add.Background=Blue;add.Foreground=Brushes.White;toolbar.Children.Add(add);
        toolbar.Children.Add(Button("事项详情",()=>OpenDetail(grid.SelectedItem as TaskItem)));
        toolbar.Children.Add(Button("新增字段",AddField));toolbar.Children.Add(Button("显示列",ShowColumns));toolbar.Children.Add(Button("清除筛选",()=>{filters.Clear();query="";search.Text="";BuildColumns();Refresh();}));
        toolbar.Children.Add(Button("导出 Excel",Export));
        var mode=new ComboBox{Width=155,Margin=new(8,4,8,4),VerticalContentAlignment=VerticalAlignment.Center,ItemsSource=new[]{"进展 · 全部展开","进展 · 选中展开","进展 · 全部折叠"},SelectedIndex=data.Mode=="all"?0:data.Mode=="collapsed"?2:1};
        mode.SelectionChanged+=(s,e)=>{data.Mode=mode.SelectedIndex==0?"all":mode.SelectedIndex==2?"collapsed":"selected";PropertyChanged?.Invoke(this,new(nameof(ProgressMode)));ScheduleSave();};toolbar.Children.Add(mode);
        search.ToolTip="搜索所有字段";search.TextChanged+=(s,e)=>{query=search.Text;Refresh();};toolbar.Children.Add(search);
        var footer=new StackPanel{Margin=new(0,12,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);saveState.Text="已从本地文档加载";footer.Children.Add(saveState);footer.Children.Add(new TextBlock{Text=store.FilePath,Foreground=Brushes.SlateGray,FontSize=11,Margin=new(0,5,0,0)});
        count.Margin=new(0,0,0,10);DockPanel.SetDock(count,Dock.Top);root.Children.Add(count);
        grid.ItemsSource=view;grid.AutoGenerateColumns=false;grid.CanUserAddRows=false;grid.CanUserDeleteRows=false;grid.CanUserReorderColumns=true;grid.SelectionMode=DataGridSelectionMode.Single;grid.SelectionUnit=DataGridSelectionUnit.FullRow;grid.HeadersVisibility=DataGridHeadersVisibility.Column;grid.GridLinesVisibility=DataGridGridLinesVisibility.Horizontal;grid.HorizontalGridLinesBrush=new SolidColorBrush(Color.FromRgb(231,236,244));grid.BorderBrush=new SolidColorBrush(Color.FromRgb(225,232,242));grid.Background=Brushes.White;grid.RowBackground=Brushes.White;grid.AlternatingRowBackground=new SolidColorBrush(Color.FromRgb(250,252,255));grid.MinRowHeight=48;grid.EnableRowVirtualization=true;grid.IsReadOnly=false;
        grid.MouseDoubleClick+=(s,e)=>{if(FindParent<TextBox>(e.OriginalSource as DependencyObject)!=null||FindParent<ComboBox>(e.OriginalSource as DependencyObject)!=null||FindParent<DatePicker>(e.OriginalSource as DependencyObject)!=null)return;if(FindParent<DataGridRow>(e.OriginalSource as DependencyObject)!=null){grid.CancelEdit();OpenDetail(grid.SelectedItem as TaskItem);e.Handled=true;}};
        grid.BeginningEdit+=(s,e)=>{if(e.Column.SortMemberPath=="Progress"&&data.Mode=="collapsed"){e.Cancel=true;OpenDetail((TaskItem)e.Row.Item);}};
        root.Children.Add(grid);BuildColumns();UpdateSummary();
        saveTimer.Tick+=(s,e)=>SaveNow();Closing+=ClosingWindow;
    }
    static T? FindParent<T>(DependencyObject? obj)where T:DependencyObject{while(obj!=null){if(obj is T t)return t;obj=VisualTreeHelper.GetParent(obj);}return null;}
    static Button Button(string text,Action action){var b=new Button{Content=text};b.Click+=(s,e)=>action();return b;}
    void TaskChanged(object? sender,PropertyChangedEventArgs e){if(e.PropertyName==nameof(TaskItem.Status))((TaskItem)sender!).ApplyStatusTimestamp();ScheduleSave();UpdateSummary();}
    void ScheduleSave(){dirty=true;saveState.Text="正在写入本地文档…";saveState.Foreground=Brushes.SlateGray;saveTimer.Stop();saveTimer.Start();}
    bool SaveNow(){saveTimer.Stop();if(!dirty)return true;try{data.Tasks=tasks.ToList();store.Save();dirty=false;saveState.Text="✓ 已自动保存到本地文档";saveState.Foreground=Brushes.SeaGreen;return true;}catch(Exception e){saveState.Text="保存失败："+e.Message+"（请重试或导出备份）";saveState.Foreground=Brushes.Firebrick;return false;}}
    void ClosingWindow(object? sender,CancelEventArgs e){if(!grid.CommitEdit(DataGridEditingUnit.Cell,true)||!grid.CommitEdit(DataGridEditingUnit.Row,true)){e.Cancel=true;MessageBox.Show(this,"请先修正表格中标红的无效输入。","输入未完成");return;}if(!SaveNow()){e.Cancel=true;MessageBox.Show(this,"本地文件未保存成功，请检查磁盘空间和权限，或导出 Excel 备份后重试。","保存失败",MessageBoxButton.OK,MessageBoxImage.Error);}}
    void UpdateSummary(){summary.Text=$"全部 {tasks.Count}     ·     进行中 {tasks.Count(t=>t.Status=="进行中")}     ·     已完成 {tasks.Count(t=>t.Status=="已完成")}     ·     挂起 {tasks.Count(t=>t.Status=="挂起")}";count.Text=$"显示 {view.Cast<TaskItem>().Count()} / {tasks.Count} 个事项　　单元格直接编辑 · 双击事项查看详情";}
    void Refresh(){grid.CommitEdit(DataGridEditingUnit.Cell,true);grid.CommitEdit(DataGridEditingUnit.Row,true);if(view is IEditableCollectionView ev&&ev.IsEditingItem)return;view.Refresh();UpdateSummary();}
    void AddTask(){filters.Clear();query="";search.Text="";BuildColumns();var t=new TaskItem();t.PropertyChanged+=TaskChanged;tasks.Insert(0,t);Refresh();grid.SelectedItem=t;grid.ScrollIntoView(t);ScheduleSave();var title=grid.Columns.FirstOrDefault(c=>c.SortMemberPath=="Title"&&c.Visibility==Visibility.Visible)??grid.Columns.FirstOrDefault(c=>!c.IsReadOnly&&c.Visibility==Visibility.Visible);if(title!=null){Dispatcher.BeginInvoke(DispatcherPriority.Input,()=>{grid.CurrentCell=new(t,title);grid.Focus();grid.BeginEdit();});}}
    static string PathFor(Field c)=>c.Key switch{"title"=>"Title","created"=>"Created","due"=>"Due","completed"=>"Completed","status"=>"Status","people"=>"PeopleText","progress"=>"Progress",_=>"["+c.Key+"]"};
    static Binding BindingFor(Field c)=>new(PathFor(c)){Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged};
    void BuildColumns(){grid.Columns.Clear();foreach(var c in data.AllFields){DataGridColumn column;
        if(c.Key=="progress"){
            var display=new FrameworkElementFactory(typeof(MarkdownView));display.SetBinding(MarkdownView.SourceProperty,new Binding("Progress"));
            var height=new MultiBinding{Converter=new ProgressHeightConverter()};height.Bindings.Add(new Binding(nameof(ProgressMode)){Source=this});height.Bindings.Add(new Binding("IsSelected"){RelativeSource=new(RelativeSourceMode.FindAncestor,typeof(DataGridRow),1)});display.SetBinding(FrameworkElement.MaxHeightProperty,height);
            var edit=new FrameworkElementFactory(typeof(TextBox));edit.SetBinding(TextBox.TextProperty,BindingFor(c));edit.SetValue(TextBox.AcceptsReturnProperty,true);edit.SetValue(TextBox.TextWrappingProperty,TextWrapping.Wrap);edit.SetValue(FrameworkElement.MinHeightProperty,100.0);
            var editPanel=new FrameworkElementFactory(typeof(StackPanel));editPanel.AppendChild(edit);var livePreview=new FrameworkElementFactory(typeof(MarkdownView));livePreview.SetBinding(MarkdownView.SourceProperty,new Binding("Progress"));livePreview.SetBinding(FrameworkElement.MaxHeightProperty,height);editPanel.AppendChild(livePreview);
            column=new DataGridTemplateColumn{CellTemplate=new(){VisualTree=display},CellEditingTemplate=new(){VisualTree=editPanel},SortMemberPath="Progress",Width=340};
        }else if(c.Type=="status"){
            var cell=new FrameworkElementFactory(typeof(ComboBox));cell.SetValue(ItemsControl.ItemsSourceProperty,Statuses);cell.SetBinding(Selector.SelectedItemProperty,BindingFor(c));cell.SetValue(FrameworkElement.MarginProperty,new Thickness(4));
            column=new DataGridTemplateColumn{CellTemplate=new(){VisualTree=cell},SortMemberPath="Status",Width=130};
        }else if(c.Type=="date"&&c.Key!="created"&&c.Key!="completed"){
            var cell=new FrameworkElementFactory(typeof(DatePicker));var b=BindingFor(c);b.Converter=new DateConverter();cell.SetBinding(DatePicker.SelectedDateProperty,b);cell.SetValue(FrameworkElement.MarginProperty,new Thickness(4));column=new DataGridTemplateColumn{CellTemplate=new(){VisualTree=cell},SortMemberPath=PathFor(c),Width=160};
        }else{
            var b=BindingFor(c);if(c.Key is "created" or "completed"){b.Mode=BindingMode.OneWay;b.Converter=new TimestampConverter();}
            if(c.Type=="number")b.ValidationRules.Add(new NumberRule());
            column=new DataGridTextColumn{Binding=b,SortMemberPath=PathFor(c),IsReadOnly=c.Key is "created" or "completed",Width=c.Key=="title"?270:c.Type=="date"?185:180};
        }
        column.Header=ColumnHeader(c);column.Visibility=data.Hidden.Contains(c.Key)?Visibility.Collapsed:Visibility.Visible;column.MinWidth=110;grid.Columns.Add(column);
    }}
    object ColumnHeader(Field c){var panel=new StackPanel{Margin=new(5,8,5,8)};panel.Children.Add(new TextBlock{Text=c.Name,FontWeight=FontWeights.SemiBold,Margin=new(2,0,0,8)});filters.TryGetValue(c.Key,out var f);f??=new();
        if(c.Type=="date"){
            var from=new DatePicker{SelectedDate=f.From,Width=115,ToolTip="起始日期（含）"};var to=new DatePicker{SelectedDate=f.To,Width=115,ToolTip="结束日期（含）",Margin=new(0,4,0,0)};
            from.SelectedDateChanged+=(s,e)=>{filters[c.Key]=new(From:from.SelectedDate,To:to.SelectedDate);Refresh();};to.SelectedDateChanged+=(s,e)=>{filters[c.Key]=new(From:from.SelectedDate,To:to.SelectedDate);Refresh();};panel.Children.Add(from);panel.Children.Add(to);
        }else if(c.Type=="people"||c.Type=="status"){
            var combo=new ComboBox{MinWidth=110,MaxWidth=200};IEnumerable<string> Options()=>new[]{"全部"}.Concat(c.Type=="status"?Statuses:tasks.SelectMany(t=>t.People).Distinct().Order());combo.ItemsSource=Options().ToArray();combo.SelectedItem=f.Text==""?"全部":f.Text;
            combo.DropDownOpened+=(s,e)=>{var selected=combo.SelectedItem;combo.ItemsSource=Options().ToArray();combo.SelectedItem=selected??"全部";};combo.SelectionChanged+=(s,e)=>{filters[c.Key]=new(combo.SelectedItem as string is {} v&&v!="全部"?v:"");Dispatcher.BeginInvoke(()=>Refresh());};panel.Children.Add(combo);
        }else{var text=new TextBox{Text=f.Text,MinWidth=110,MaxWidth=260,ToolTip="筛选"+c.Name,Padding=new(4)};text.TextChanged+=(s,e)=>{filters[c.Key]=new(text.Text);Refresh();};panel.Children.Add(text);}return panel;
    }
    void OpenDetail(TaskItem? t){if(t==null)return;grid.CommitEdit(DataGridEditingUnit.Cell,true);grid.CommitEdit(DataGridEditingUnit.Row,true);var dialog=Dialog("事项详情",720,800);var outer=new DockPanel{Margin=new(24)};dialog.Content=outer;
        var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Bottom);outer.Children.Add(actions);
        actions.Children.Add(Button("删除事项",()=>{if(MessageBox.Show(dialog,"确定删除此事项？此操作无法撤销。","删除事项",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes){tasks.Remove(t);t.PropertyChanged-=TaskChanged;ScheduleSave();Refresh();dialog.Close();}}));actions.Children.Add(Button("完成",()=>{dialog.Close();Refresh();}));
        var form=new StackPanel();outer.Children.Add(new ScrollViewer{Content=form,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});form.Children.Add(new TextBlock{Text="事项详情",FontSize=24,FontWeight=FontWeights.SemiBold,Margin=new(0,0,0,8)});form.Children.Add(new TextBlock{Text="修改自动保存 · Markdown 实时预览",Foreground=Brushes.SlateGray});
        foreach(var c in data.AllFields){form.Children.Add(new TextBlock{Text=c.Name,Foreground=Brushes.SlateGray,Margin=new(0,18,0,7)});FrameworkElement editor;
            if(c.Key=="progress"){
                var stack=new StackPanel();var input=new TextBox{AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=120,MaxHeight=260,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Padding=new(8)};input.SetBinding(TextBox.TextProperty,BindingFor(c));stack.Children.Add(input);var preview=new MarkdownView{MinHeight=90,Margin=new(0,10,0,0)};preview.SetBinding(MarkdownView.SourceProperty,new Binding("Progress"));stack.Children.Add(preview);editor=stack;
            }else if(c.Type=="status"){var combo=new ComboBox{ItemsSource=Statuses};combo.SetBinding(Selector.SelectedItemProperty,BindingFor(c));editor=combo;}
            else if(c.Type=="date"&&c.Key!="created"&&c.Key!="completed"){var picker=new DatePicker();var b=BindingFor(c);b.Converter=new DateConverter();picker.SetBinding(DatePicker.SelectedDateProperty,b);editor=picker;}
            else {var input=new TextBox{Padding=new(8),IsReadOnly=c.Key is "created" or "completed"};var b=BindingFor(c);if(input.IsReadOnly){b.Mode=BindingMode.OneWay;b.Converter=new TimestampConverter();}if(c.Type=="number")b.ValidationRules.Add(new NumberRule());input.SetBinding(TextBox.TextProperty,b);editor=input;}
            editor.DataContext=t;form.Children.Add(editor);
        }dialog.Closed+=(s,e)=>{Refresh();SaveNow();};dialog.ShowDialog();
    }
    Window Dialog(string title,double width,double height)=>new(){Title=title,Owner=this,Width=width,Height=height,WindowStartupLocation=WindowStartupLocation.CenterOwner,FontFamily=FontFamily,FontSize=13,Background=Brushes.White};
    void AddField(){var dialog=Dialog("新增字段",420,300);var panel=new StackPanel{Margin=new(24)};dialog.Content=panel;panel.Children.Add(new TextBlock{Text="字段名称",Margin=new(0,0,0,8)});var name=new TextBox{Padding=new(8),MaxLength=40};panel.Children.Add(name);var type=new ComboBox{ItemsSource=new[]{"文本","数字","日期"},SelectedIndex=0,Margin=new(0,16,0,16)};panel.Children.Add(type);panel.Children.Add(Button("创建字段",()=>{var text=name.Text.Trim();if(text==""||data.AllFields.Any(c=>c.Name==text)){MessageBox.Show(dialog,"请输入不重复的字段名称。");return;}data.Columns.Add(new("custom_"+Guid.NewGuid(),text,type.SelectedIndex==1?"number":type.SelectedIndex==2?"date":"text"));BuildColumns();ScheduleSave();dialog.Close();}));dialog.ShowDialog();}
    void ShowColumns(){var dialog=Dialog("显示列",360,520);var panel=new StackPanel{Margin=new(24)};dialog.Content=new ScrollViewer{Content=panel};panel.Children.Add(new TextBlock{Text="隐藏列不会删除数据",Foreground=Brushes.SlateGray,Margin=new(0,0,0,15)});foreach(var c in data.AllFields){var cb=new CheckBox{Content=c.Name,IsChecked=!data.Hidden.Contains(c.Key),Margin=new(0,9,0,9)};cb.Click+=(s,e)=>{if(cb.IsChecked==true)data.Hidden.Remove(c.Key);else if(data.AllFields.Count-data.Hidden.Count>1)data.Hidden.Add(c.Key);else{cb.IsChecked=true;return;}foreach(var column in grid.Columns)if(column.SortMemberPath==PathFor(c))column.Visibility=cb.IsChecked==true?Visibility.Visible:Visibility.Collapsed;ScheduleSave();};panel.Children.Add(cb);}dialog.ShowDialog();}
    void Export(){if(!grid.CommitEdit(DataGridEditingUnit.Cell,true)||!grid.CommitEdit(DataGridEditingUnit.Row,true)){MessageBox.Show(this,"请先修正表格中标红的无效输入。");return;}var picker=new SaveFileDialog{Title="导出当前筛选结果",Filter="Excel 工作簿 (*.xlsx)|*.xlsx",FileName=$"事项簿_{DateTime.Now:yyyy-MM-dd}.xlsx"};if(picker.ShowDialog(this)!=true)return;try{ExcelExport.Write(picker.FileName,data.AllFields,view.Cast<TaskItem>().ToList());MessageBox.Show(this,"已导出当前筛选结果，包含所有字段及完整参与人。","导出完成");}catch(Exception e){MessageBox.Show(this,e.Message,"导出失败",MessageBoxButton.OK,MessageBoxImage.Error);}}
}
public class ProgressHeightConverter:IMultiValueConverter{public object Convert(object[] v,Type t,object p,CultureInfo c)=>v[0] as string=="all"||v[0] as string=="selected"&&v[1] is true?double.PositiveInfinity:54.0;public object[] ConvertBack(object v,Type[] t,object p,CultureInfo c)=>throw new NotSupportedException();}
public class DateConverter:IValueConverter{public object? Convert(object v,Type t,object p,CultureInfo c)=>DateTime.TryParse(v as string,out var date)?date:(DateTime?)null;public object ConvertBack(object v,Type t,object p,CultureInfo c)=>v is DateTime date?date.ToString("yyyy-MM-dd"):"";}
public class TimestampConverter:IValueConverter{public object Convert(object v,Type t,object p,CultureInfo c)=>DateTimeOffset.TryParse(v as string,out var date)?date.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"):"—";public object ConvertBack(object v,Type t,object p,CultureInfo c)=>Binding.DoNothing;}
public class NumberRule:ValidationRule{public override ValidationResult Validate(object value,CultureInfo culture)=>string.IsNullOrWhiteSpace(value as string)||double.TryParse(value as string,NumberStyles.Number,culture,out _)?ValidationResult.ValidResult:new(false,"请输入数字");}
