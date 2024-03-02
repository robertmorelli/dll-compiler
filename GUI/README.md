```
Author:     Robert Morelli, Noah Yeh
Partner:    Noah Yeh, Robert Morelli
Start Date: 1-7-24
Course:     CS 3505, University of Utah, School of Computing
GitHub ID:  robertmorelli, snoahhhh
Repo:       https://github.com/uofu-cs3500-spring24/assignment-six-gui-functioning-spreadsheet-team
Commit Date: 3-2-24
Solution:   Spreadsheet
Copyright:  CS 3500, Robert Morelli, and Noah Yeh - This work may not be copied for use in Academic Coursework.
```

# Comments to Evaluators:

Our special feature is a Spreadsheet as a compiled .NET language
you can export your spreadsheet to a `.dll` and execute
optimized and compiled spreadsheet formulas in any .NET environment.

### REQUIREMENTS TO RUN THIS FEATURE: .NET 9 preview
#### ---> [link to why](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.assemblybuilder.save?view=net-9.0#system-reflection-emit-assemblybuilder-save(system-string)) <---
#### You must:
- [Download the .NET9 runtime preview](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- you may also need to download Visual Studio Preview from your Visual Studio Installer
- Ensure `global.json` either says `9` as the framework number or `"allowPrerelease"` is `true`
- You may have to run some of these:
  - `dotnet workload install maui`
  - `dotnet workload install maui-maccatalyst`
  - `dotnet workload install maccatalyst`
  - `dotnet workload restore`

### What actually is a compiled spreadsheet?
- The `.dll` will contain one namespace `sheetSpace` with one class type called `sheetLibrary`
- All cells that return a `"value"` that is a `double` will result in a `private` `double _{cellname}` and a `public` `double Get_{cellname}`
- All cells that are constant value doubles and not formulas (for example not including `=25`) will result in `public` `void Put_{cellname}`
- Additionally, `private` `void Compute_{cellname}` exist for all formula based cells and these are used to recalculate cells on a `Put` of any dependency

here is a test that may show what exactly this means:
```c#
public void CompileTest()
{
    Spreadsheet.Spreadsheet sheet = new();
    sheet.SetContentsOfCell("a4", "=  a3 + 20 * 10");
    sheet.SetContentsOfCell("a3", "=  a2 + 6 * 30");
    sheet.SetContentsOfCell("a2", "=  a1 / 2");
    sheet.SetContentsOfCell("a1", "4");
    sheet.GetCellContents("a3");
    
    //compile to location
    var location = sheet.Compile("name");
    
    //load back in and get type data
    var nameAssembly = Assembly.LoadFile(location);
    var instance = nameAssembly.CreateInstance("sheetSpace.sheetLibrary");
    var sheetType = instance?.GetType();
    
    //get methods to test
    var getA4 = sheetType?.GetMethod("Get_a4");
    var setA1 = sheetType?.GetMethod("Put_a1");
    
    //check constructor puts default values
    Assert.AreEqual(sheet.GetCellValue("a4"), getA4?.Invoke(instance, []));
    
    //change value for both
    setA1?.Invoke(instance, [5]);
    //calls hidden function to update dependent cells
    sheet.SetContentsOfCell("a1", "5");
    
    //check values are updated properly
    Assert.AreEqual(sheet.GetCellValue("a4"), getA4?.Invoke(instance, []));
}
```
The resulting assembly can be decompiled into this (by JetBrains Rider):
```c#
// Decompiled with JetBrains decompiler
// Type: sheetSpace.sheetLibrary
// Assembly: nameAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 859585CC-F549-4A37-81A0-8E30A7547E4B
// Assembly location: /Users/robertmorelli/Documents/personal-repos/assignment-six-gui-functioning-spreadsheet-team/Solution Items/name.dll
// Compiler-generated code is shown

using System.Runtime.InteropServices;

namespace sheetSpace
{
  public class sheetLibrary
  {
    private double _a4;
    private double _a3;
    private double _a2;
    private double _a1;

    public double Get_a4()
    {
      return this._a4;
    }

    public double Get_a3()
    {
      return this._a3;
    }

    public double Get_a2()
    {
      return this._a2;
    }

    public double Get_a1()
    {
      return this._a1;
    }

    public sheetLibrary()
    {
      this._a4 = 382.0;
      this._a3 = 182.0;
      this._a2 = 2.0;
      this._a1 = 4.0;
    }

    private void Compute_a4()
    {
      this._a4 = 200.0 + this._a3;
    }

    private void Compute_a3()
    {
      this._a3 = 180.0 + this._a2;
    }

    private void Compute_a2()
    {
      this._a2 = 0.5 * this._a1;
    }

    public void Put_a1([In] double obj0)
    {
      this._a1 = obj0;
      this.Compute_a2();
      this.Compute_a3();
      this.Compute_a4();
    }
  }
}
```
You can run this test with `dotnet test SpreadsheetTests --filter "CompileTest"` (It will create `name.dll` on your desktop). The net9 cli might also be broken so maybe run in through a UI



# Time Expenditure:
    - Assignment Six: Predicted Hours:          10       Actual Hours:   14
