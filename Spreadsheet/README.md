```
Author:     Robert Morelli
Partner:    None
Start Date: 1-7-24
Course:     CS 3505, University of Utah, School of Computing
GitHub ID:  robertmorelli
Repo:       https://github.com/uofu-cs3500-spring24/assignment-six-gui-functioning-spreadsheet-team
Commit Date: 2-8-24
Solution:   Spreadsheet
Copyright:  CS 3500 and Robert Morelli - This work may not be copied for use in Academic Coursework.
```

# Comments to Evaluators:
If you want to implement this recursively do this (doesn't check circularity tho)
```
Stack<string> GetCellsToRecalculate(string name)
{
    Stack<string> changed = new();
    HashSet<string> visited = [name];
    void Visit(string toVisit)
    {
        foreach (string n in GetDirectDependents(toVisit))
            if (visited.Add(n)) Visit(n);
        changed.Push(toVisit);
    }
    Visit(name);
    return changed;
}
```

# Time Expenditure:
    - Assignment Four: Predicted Hours:          5       Actual Hours:   7

    - Assignment five:  Predicted Hours:   5 code 5 debug     Actual Hours:   2 code 6 debug
