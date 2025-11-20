## Introduction

**A lightweight Json.NET–based serialization library that supports object identity, circular references, and per-object custom serialization.**

Each serializable type can implement  
`GetObjectData(SerializableObjectInfo info)` and  
`SetObjectData(SerializableObjectInfo info)`  
to fully control how its data is stored and restored.

This follows an *ISerializable-style custom serialization pattern*, enabling precise control over complex object graphs, shared references, and state reconstruction.
```C#
var fruits = new Category() { Name = "Fruits" };
var coffee = new Category() { Name = "Coffee" };

Store store = new Store();
store.Categories.Add(fruits);
store.Categories.Add(coffee);

Product apple = new Product() { Name = "Apple", Category = fruits };
Product banana = new Product() { Name = "Banana", Category = fruits };

store.Products.Add(apple);
store.Products.Add(banana);

Product americano = new Product() { Name = "Americano", Category = coffee };
Product moca = new Product() { Name = "Moca", Category = coffee };
            
store.Products.Add(americano);
store.Products.Add(moca);

string json = Jay.Serialization.Serializer.Serialize(store);

Store store2 = Jay.Serialization.Serializer.Deserialize<Store>(json);

foreach (var category in store2.Categories)
{
    Console.WriteLine($"{category.Name}");
}

foreach (var product in store2.Products)
{
    Console.WriteLine($"{product.Category.Name}, {product.Name}");
}

Console.WriteLine(store2.Products[0].Category == store2.Products[1].Category);

// Fruits
// Coffee
// Fruits, Apple
// Fruits, Banana
// Coffee, Americano
// Coffee, Moca
// True
```

```C#
internal class Store : ISerializableObject
{
    public List<Category> Categories { get; set; } = new List<Category>();
    public List<Product> Products { get; set; } = new List<Product>();

    //requires empty constructor
    public Store()
    {
            
    }

    public void GetObjectData(SerializableObjectInfo info)
    {
        info.AddValue(nameof(Categories), Categories);
        info.AddValue(nameof(Products), Products);
    }
    public void SetObjectData(SerializableObjectInfo info)
    {
        Categories = info.GetValue<List<Category>>(nameof(Categories))!;
        Products = info.GetValue<List<Product>>(nameof(Products))!;
    }
}

internal class Category : ISerializableObject
{
    public string Name { get; set; } = null!;

    //requires empty constructor
    public Category()
    {
            
    }

    public void GetObjectData(SerializableObjectInfo info)
    {
        info.AddValue(nameof(Name), Name);
    }

    public void SetObjectData(SerializableObjectInfo info)
    {
        Name = info.GetValue<string>(nameof(Name))!;
    }
}

internal class Product : ISerializableObject
{
    public string Name { get; set; } = null!;

    public Category Category { get; set; } = null!;

    //requires empty constructor
    public Product()
    {
            
    }
    public void GetObjectData(SerializableObjectInfo info)
    {
        info.AddValue(nameof(Name), Name);
        info.AddValue(nameof(Category), Category);
    }

    public void SetObjectData(SerializableObjectInfo info)
    {
        Name = info.GetValue<string>(nameof(Name))!;
        Category = info.GetValue<Category>(nameof(Category))!;
    }
}
```
