using System.IO.Compression;
using System.Xml.Linq;
namespace Shixiang;
public static class ExcelExport
{
    public static void Write(string path,IReadOnlyList<Field> fields,IEnumerable<TaskItem> tasks)
    {
        using var output=File.Create(path);using var zip=new ZipArchive(output,ZipArchiveMode.Create);
        void Part(string name,XDocument xml){using var s=zip.CreateEntry(name).Open();xml.Save(s);}
        XNamespace ct="http://schemas.openxmlformats.org/package/2006/content-types",rel="http://schemas.openxmlformats.org/package/2006/relationships",s="http://schemas.openxmlformats.org/spreadsheetml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        Part("[Content_Types].xml",new(new XElement(ct+"Types",new XElement(ct+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),new XElement(ct+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),new XElement(ct+"Override",new XAttribute("PartName","/xl/workbook.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),new XElement(ct+"Override",new XAttribute("PartName","/xl/worksheets/sheet1.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
        XElement Relation(string id,string type,string target)=>new(rel+"Relationship",new XAttribute("Id",id),new XAttribute("Type",r.NamespaceName+"/"+type),new XAttribute("Target",target));
        Part("_rels/.rels",new(new XElement(rel+"Relationships",Relation("rId1","officeDocument","xl/workbook.xml"))));
        Part("xl/workbook.xml",new(new XElement(s+"workbook",new XAttribute(XNamespace.Xmlns+"r",r),new XElement(s+"sheets",new XElement(s+"sheet",new XAttribute("name","事项"),new XAttribute("sheetId","1"),new XAttribute(r+"id","rId1"))))));
        Part("xl/_rels/workbook.xml.rels",new(new XElement(rel+"Relationships",Relation("rId1","worksheet","worksheets/sheet1.xml"))));
        var rows=new XElement(s+"sheetData");var values=new List<string[]>{fields.Select(c=>c.Name).ToArray()};values.AddRange(tasks.Select(t=>fields.Select(c=>AppData.Value(t,c.Key)).ToArray()));
        for(int row=0;row<values.Count;row++){var el=new XElement(s+"row",new XAttribute("r",row+1));for(int col=0;col<fields.Count;col++){var text=new string(values[row][col].Where(ch=>System.Xml.XmlConvert.IsXmlChar(ch)).ToArray());el.Add(new XElement(s+"c",new XAttribute("r",ColumnName(col)+(row+1)),new XAttribute("t","inlineStr"),new XElement(s+"is",new XElement(s+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text))));}rows.Add(el);}
        Part("xl/worksheets/sheet1.xml",new(new XElement(s+"worksheet",rows)));
    }
    static string ColumnName(int n){string name="";for(n++;n>0;n=(n-1)/26)name=(char)('A'+(n-1)%26)+name;return name;}
}
