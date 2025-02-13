using MultiKeyLookup;
using System;
using System.Collections.Generic;
using System.Linq;

DataClass[] Data = new DataClass[]
{
    new DataClass() { A = 1, B = "2", C = 3 },
    new DataClass() { A = 1, B = "3", C = 2 },
    new DataClass() { A = 3, B = "1", C = 1 }
};

MultiKeyLookup<DataClass> Indexed = new(Data, ("A", f => f.A), ("B", f => f.B));

IEnumerable<DataClass> A1 = Indexed.Get("A", 1);
Console.WriteLine($"A1 {string.Join(',', A1.Select(s => s.ToString()))}");

IEnumerable<DataClass> B2 = Indexed.Get("B", "2");
Console.WriteLine($"B2 {string.Join(',', B2.Select(s => s.ToString()))}");

DataClass C = new DataClass() { A = 2, B = "1", C = 3 };
Indexed.Add(C);
IEnumerable<DataClass> C3 = Indexed.Get("C", 3, f => f.C);
Console.WriteLine($"C3 {string.Join(',', C3.Select(s => s.ToString()))}");

Indexed.Remove("B", 2);
Console.WriteLine($"B2 {string.Join(',', B2.Select(s => s.ToString()))}");

Console.ReadLine();

class DataClass
{
    public int A { get; set; }
    public string B { get; set; } = "";
    public int C { get; set; }

    public override string ToString()
    {
        return $"{A}_{B}_{C}";
    }
}