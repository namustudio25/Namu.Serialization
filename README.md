## Introduction

**A Json.NET–based serialization library that supports object identity, circular references, and per-object custom serialization.**

Each serializable type can implement  
`GetObjectData(SerializableObjectInfo info)` and  
`SetObjectData(SerializableObjectInfo info)`  
to fully control how its data is stored and restored.

This follows an *ISerializable-style custom serialization pattern*, enabling precise control over complex object graphs, shared references, and state reconstruction.

## Usage Example
```C#
Table table = new Table() { Name = "MyTable" };
var row1 = table.NewRow();
table.AddRow(row1);

Console.WriteLine(object.ReferenceEquals(table, table.Rows[0].Table));
Console.WriteLine(object.ReferenceEquals(table.Rows[0], table.Rows[1]));
// True
// True

string json = Namu.Serialization.Serializer.Serialize(table);

Table table2 = Namu.Serialization.Serializer.Deserialize<Table>(json);

Console.WriteLine(object.ReferenceEquals(table, table.Rows[0].Table));
Console.WriteLine(object.ReferenceEquals(table.Rows[0], table.Rows[1]));
// True
// True
```

## Customization Example
```C#
class Row : ISerializableObject
{
    //public string Name { get; set; } = null!;// ver1
    public string Name2 { get; set; } = null!;// ver2

    public Table Table { get; set; } = null!;

    public void GetObjectData(SerializableObjectInfo info)
    {
        info.AddValue("Version", 2);
        info.AddValue(nameof(Name2), Name2);
        info.AddValue(nameof(Table), Table);
    }

    public void SetObjectData(SerializableObjectInfo info)
    {
        var version = info.GetValue<int>("Version");

        if (version == 1)
            Name2 = info.GetValue<string>("Name");
        else
            Name2 = info.GetValue<string>(nameof(Name2));

        Table = info.GetValue<Table>(nameof(Table));
    }
}

class Table : ISerializableObject
{
    public string Name { get; set; } = null!;

    public List<Row> Rows { get; set; } = new List<Row>();

    public Row NewRow()
    { 
        Row row = new Row();
        row.Table = this;
        Rows.Add(row);
        return row;
    }
        
    public void AddRow(Row row)
    {
        row.Table = this;
        Rows.Add(row);
    }

    public void GetObjectData(SerializableObjectInfo info)
    {
        info.AddValue(nameof(Name), Name);
        info.AddValue(nameof(Rows), Rows);            
    }

    public void SetObjectData(SerializableObjectInfo info)
    {
        Name = info.GetValue<string>(nameof(Name));
        Rows = info.GetValue<List<Row>>(nameof(Rows));
    }
}
```
