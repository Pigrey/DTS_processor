using System.Text.Json;
using System.Text.Json.Serialization;
namespace Shixiang;
public class Envelope
{
    [JsonPropertyName("revision")]public int Revision{get;set;}
    [JsonPropertyName("data")]public AppData Data{get;set;}=new();
}
public sealed class FileStore : IDisposable
{
    readonly FileStream fileLock;
    readonly JsonSerializerOptions options=new(){WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping};
    public string FilePath {get;}
    public Envelope State {get;private set;}
    public FileStore(string directory)
    {
        Directory.CreateDirectory(directory);FilePath=Path.Combine(directory,"tasks.json");
        fileLock=new FileStream(FilePath+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        try {
            if(File.Exists(FilePath)){
                var text=File.ReadAllText(FilePath);
                using var document=JsonDocument.Parse(text);
                var data=document.RootElement.GetProperty("data");
                if(data.GetProperty("tasks").ValueKind!=JsonValueKind.Array||data.GetProperty("columns").ValueKind!=JsonValueKind.Array)throw new InvalidDataException("事项文件格式不正确，原文件已保留。");
                State=JsonSerializer.Deserialize<Envelope>(text,options)??throw new InvalidDataException("数据为空。");
                if(State.Data.Tasks==null||State.Data.Columns==null||State.Data.Hidden==null)throw new InvalidDataException("数据格式异常。");
                foreach(var t in State.Data.Tasks){t.Custom??=[];if(t.Id==null||t.Created==null)throw new InvalidDataException("事项字段损坏。");}
            }else State=new();
        }catch{fileLock.Dispose();throw;}
    }
    public void Save()
    {
        var next=new Envelope{Revision=State.Revision+1,Data=State.Data};var temp=FilePath+"."+Guid.NewGuid()+".tmp";
        try{
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){JsonSerializer.Serialize(stream,next,options);stream.Flush(true);}
            if(File.Exists(FilePath))File.Replace(temp,FilePath,FilePath+".bak",true);else File.Move(temp,FilePath);
            State.Revision=next.Revision;
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public void Dispose()=>fileLock.Dispose();
}
