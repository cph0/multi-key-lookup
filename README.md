# multi-key-lookup
A data structure that allows indexing by multiple fields
for fast data access.

## Basic Usage

Pass the data and an array of index fields to the constructor.
Get the data with a field and value.

```C#

public class DataClass
{
  public int A { get; set; }
  public int B { get; set; }
  public int C { get; set; }
}

public void Get() 
{
    var Data = new DataClass[] {new DataClass() { A = 1, B = 2, C = 3 }, 
    new DataClass() { A = 1, B = 3, C = 2 }, new DataClass(){ A = 3, B = 2, C = 1}};
    var Indexed = new MultiKeyLookup(Data, ("A", k => k.A), ("B", k => k.B));

    IEnumerable<DataClass> A1 = Indexed.get('A', 1);
    IEnumerable<DataClass> B2 = Indexed.get('B', 2);

    //prints [{a: 1, b: 2, c: 3}, {a: 1, b: 3, c: 2}]
    Console.WriteLine(A1);

    //prints [{a: 1, b: 2, c: 3}, {a: 3, b: 2, c: 1}]
    Console.WriteLine(B2);
}
```

## API

```C#
new MultiKeyLookup(data, ...indexes); //data and indexes are optional
Count; //the number of data entries
Indexes; //the number of indexes
Values; //gets an iterable of values
AddIndex((field, key)); //indexes the data by another index
RemoveIndex(field); //removes an index
Add(...data); //adds data
ContainsKey(field, value, key); //does the data exist by field and value, key needed for no index
TryGetValue(field, value, key); //gets the data by field and value, key needed for no index
Get(field, value, key); //gets the data by field and value, key needed for no index
Remove(field, value, key); //removes the data by field and value, key needed for no index
```
