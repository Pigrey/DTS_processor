using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shixiang;
public class TaskItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void Set<T>(ref T field, T value, [CallerMemberName] string? name=null) {if(EqualityComparer<T>.Default.Equals(field,value))return;field=value;PropertyChanged?.Invoke(this,new(name));}
    string title="",due="",completed="",status="进行中",progress="";
    List<string> people=["王淞晖"];
    [JsonPropertyName("id")] public string Id {get;set;}=Guid.NewGuid().ToString();
    [JsonPropertyName("created")] public string Created {get;set;}=DateTimeOffset.Now.ToString("O");
    [JsonPropertyName("title")] public string Title {get=>title;set=>Set(ref title,value??"");}
    [JsonPropertyName("due")] public string Due {get=>due;set=>Set(ref due,value??"");}
    [JsonPropertyName("completed")] public string Completed {get=>completed;set=>Set(ref completed,value??"");}
    [JsonPropertyName("status")] public string Status {get=>status;set=>Set(ref status,value??"进行中");}
    [JsonPropertyName("progress")] public string Progress {get=>progress;set=>Set(ref progress,value??"");}
    [JsonPropertyName("people")] public List<string> People {get=>people;set{people=value??[];PropertyChanged?.Invoke(this,new(nameof(PeopleText)));}}
    [JsonIgnore] public string PeopleText {get=>string.Join("、",People);set{People=value.Split(['、',',','，',';','；'],StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();}}
    [JsonPropertyName("custom")] public Dictionary<string,string> Custom {get;set;}=[];
    public string this[string key] {get=>Custom.GetValueOrDefault(key,"");set{Custom[key]=value;PropertyChanged?.Invoke(this,new("Item[]"));}}
    public void ApplyStatusTimestamp()=>Completed=Status=="已完成"?DateTimeOffset.Now.ToString("O"):"";
}
public record Field([property:JsonPropertyName("key")] string Key,[property:JsonPropertyName("name")] string Name,[property:JsonPropertyName("type")] string Type);
public class AppData
{
    [JsonPropertyName("tasks")] public List<TaskItem> Tasks {get;set;}=[];
    [JsonPropertyName("columns")] public List<Field> Columns {get;set;}=[];
    [JsonPropertyName("hidden")] public List<string> Hidden {get;set;}=[];
    [JsonPropertyName("mode")] public string Mode {get;set;}="selected";
    public static List<Field> BuiltIn => [new("title","事项","text"),new("created","创建时间","date"),new("due","截止时间","date"),new("completed","实际完成时间","date"),new("status","状态","status"),new("people","参与人","people"),new("progress","当前进展","text")];
    [JsonIgnore] public List<Field> AllFields=>[..BuiltIn,..Columns];
    public static string Value(TaskItem task,string key)=>key switch {"title"=>task.Title,"created"=>task.Created,"due"=>task.Due,"completed"=>task.Completed,"status"=>task.Status,"people"=>task.PeopleText,"progress"=>task.Progress,_=>task[key]};
}
public record Filter(string Text="",DateTime? From=null,DateTime? To=null);
public static class TaskFilter
{
    public static bool Matches(TaskItem t,IEnumerable<Field> fields,Dictionary<string,Filter> filters,string query)
    {
        var cols=fields.ToList();if(!string.IsNullOrWhiteSpace(query)&&!cols.Any(c=>AppData.Value(t,c.Key).Contains(query,StringComparison.OrdinalIgnoreCase)))return false;
        foreach(var c in cols){if(!filters.TryGetValue(c.Key,out var f))continue;var value=AppData.Value(t,c.Key);
            if(c.Type=="date"){if(f.From==null&&f.To==null)continue;if(!DateTimeOffset.TryParse(value,out var d))return false;var day=value.Length==10?DateTime.Parse(value).Date:d.LocalDateTime.Date;if(f.From!=null&&day<f.From.Value.Date||f.To!=null&&day>f.To.Value.Date)return false;}
            else if(c.Type=="people"){if(f.Text!=""&&!t.People.Contains(f.Text))return false;}
            else if(c.Type=="status"){if(f.Text!=""&&value!=f.Text)return false;}
            else if(!value.Contains(f.Text,StringComparison.OrdinalIgnoreCase))return false;
        }return true;
    }
}
