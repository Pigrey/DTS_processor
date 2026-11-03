using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace Shixiang;
public static class Program
{
    [STAThread] public static void Main(){
        var app=new Application();
        app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show(e.Exception.Message,"事项簿 · 操作失败",MessageBoxButton.OK,MessageBoxImage.Error);e.Handled=true;};
        try{
            app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("Theme.xaml",UriKind.Relative)});
            var windowStyle=(Style)app.Resources[typeof(Window)];
            windowStyle.Setters.Add(new Setter(Window.IconProperty,System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Assets/App.ico"))));
            string directory=Environment.GetEnvironmentVariable("TASK_TRACKER_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"事项簿");
            using var store=new FileStore(directory);
            app.Run(new MainWindow(store){Icon=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Assets/App.ico"))});
        }catch(Exception e){MessageBox.Show("无法打开事项簿。\n\n"+e.Message+"\n\n请关闭旧版或其他事项簿窗口后重试。原数据文件不会被覆盖。","事项簿",MessageBoxButton.OK,MessageBoxImage.Error);}
    }
}
