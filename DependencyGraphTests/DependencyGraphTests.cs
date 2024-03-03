/// <summary>
/// Author:    Robert Morelli
/// Partner:   None
/// Date:      2-8-24
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500 and [Your Name(s)] - This work may not 
///            be copied for use in Academic Coursework.
///
/// I, Robert Morelli, certify that I wrote this code from scratch and
/// did not copy it in part or whole from code that is not my own. All 
/// references to code that is not my own used in the completion of the
/// assignments are cited in my README file.
///
/// File Contents:
///     tests for the dependency graph
///     first 20 tests should test every comination of paths through the code
/// </summary>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;

namespace DevelopmentTests;

/// <summary>
///     This is a test class for DependencyGraphTest and is intended
///     to contain all DependencyGraphTest Unit Tests
/// </summary>
[TestClass]
public class DependencyGraphTests
{
    /// <summary>
    ///     empty is zero
    /// </summary>
    [TestMethod]
    public void NothingIsVoid()
    {
        var t = new DependencyGraph();
        Assert.AreEqual(0, t.Size);
    }

    /// <summary>
    ///     empty is zero but cover other func
    /// </summary>
    [TestMethod]
    public void NothingHasNoDependentsForA()
    {
        var t = new DependencyGraph();
        Assert.AreEqual(0, t["a"]);
    }

    /// <summary>
    ///     empty is zero but check a third way
    /// </summary>
    [TestMethod]
    public void NothingHasNoDependeesForA()
    {
        var t = new DependencyGraph();
        Assert.AreEqual(false, t.HasDependents("a"));
    }

    /// <summary>
    ///     empty is 0 but check by dependees
    /// </summary>
    [TestMethod]
    public void NothingHasNoDependeesForA2()
    {
        var t = new DependencyGraph();
        Assert.AreEqual(false, t.HasDependees("a"));
    }

    /// <summary>
    ///     empty is zero but check enumerables
    /// </summary>
    [TestMethod]
    public void NothingHasNoDependentsForA2()
    {
        var t = new DependencyGraph();
        var t2 = t.GetDependents("a");
        Assert.AreEqual(0, t2.Count());
    }

    /// <summary>
    ///     empty is zero but check dependee enumerable
    /// </summary>
    [TestMethod]
    public void NothingHasNoDependendeesForA3()
    {
        var t = new DependencyGraph();
        var t2 = t.GetDependees("a");
        Assert.AreEqual(0, t2.Count());
    }

    /// <summary>
    ///     empty is zero check brackets
    /// </summary>
    [TestMethod]
    public void DependencyGraphCoverageTest()
    {
        var t = new DependencyGraph();
        t.RemoveDependency("a", "a");
        Assert.AreEqual(0, t["a"]);
    }

    /// <summary>
    ///     empty is zero check dependency replacement for null
    /// </summary>
    [TestMethod]
    public void NothingHasNoDependendeesForA5()
    {
        var t = new DependencyGraph();
        t.ReplaceDependents("a", []);
        Assert.AreEqual(0, t["a"]);
    }


    /// <summary>
    ///     empty is zero but check replacement of dependees
    /// </summary>
    [TestMethod]
    public void NothingHasNoDependendeesForA6()
    {
        var t = new DependencyGraph();
        t.ReplaceDependees("a", []);
        Assert.AreEqual(0, t["a"]);
    }


    /// <summary>
    ///     one is one check
    /// </summary>
    [TestMethod]
    public void JustAHasSomeDependendeesForA()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        Assert.AreEqual(1, t.Size);
    }

    /// <summary>
    ///     one is one check brackets
    /// </summary>
    [TestMethod]
    public void JustAHasSomeDependendeesForA1()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        Assert.AreEqual(1, t["a"]);
    }

    /// <summary>
    ///     one is one check has dependent
    /// </summary>
    [TestMethod]
    public void JustAHasSomeDependendeesForA2()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        Assert.AreEqual(true, t.HasDependents("a"));
    }

    /// <summary>
    ///     one is one check has dependees
    /// </summary>
    [TestMethod]
    public void JustAHasSomeDependendeesForA3()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        Assert.AreEqual(true, t.HasDependees("a"));
    }

    /// <summary>
    ///     one is one check dependent enumerable
    /// </summary>
    [TestMethod]
    public void JustAHasSomeDependendeesForA4()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        var t2 = t.GetDependents("a");
        Assert.AreEqual(1, t2.Count());
    }

    /// <summary>
    ///     one is one check dependee enumerable
    /// </summary>
    [TestMethod]
    public void JustAHasSomeDependendeesForA5()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        var t2 = t.GetDependees("a");
        Assert.AreEqual(1, t2.Count());
    }

    /// <summary>
    ///     one to zero my remove
    /// </summary>
    [TestMethod]
    public void NothingAddThenRemoveASHouldBe1()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        t.RemoveDependency("a", "a");
        Assert.AreEqual(0, t["a"]);
    }

    /// <summary>
    ///     one to one by replacements
    /// </summary>
    [TestMethod]
    public void NothingAddThenRemoveASHouldBe12()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        t.ReplaceDependents("a", ["a"]);
        Assert.AreEqual(1, t["a"]);
    }

    /// <summary>
    ///     one to zero by replacements
    /// </summary>
    [TestMethod]
    public void NothingAddThenRemoveASHouldBe17()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        t.ReplaceDependents("a", []);
        Assert.AreEqual(0, t["a"]);
    }

    /// <summary>
    ///     one to one by replacement dependee
    /// </summary>
    [TestMethod]
    public void NothingAddThenRemoveASHouldBe13()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        t.ReplaceDependees("a", ["a"]);
        Assert.AreEqual(1, t["a"]);
    }

    /// <summary>
    ///     one to z by replacement dependee
    /// </summary>
    [TestMethod]
    public void NothingAddThenRemoveASHouldBe19()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "a");
        t.ReplaceDependees("a", []);
        Assert.AreEqual(0, t["a"]);
    }

    /// <summary>
    ///     Empty graph should contain nothing
    /// </summary>
    [TestMethod]
    public void SimpleEmptyTest()
    {
        var t = new DependencyGraph();
        Assert.AreEqual(0, t.Size);
    }


    /// <summary>
    ///     Empty graph should contain nothing
    /// </summary>
    [TestMethod]
    public void SimpleEmptyRemoveTest()
    {
        var t = new DependencyGraph();
        t.AddDependency("x", "y");
        Assert.AreEqual(1, t.Size);
        t.RemoveDependency("x", "y");
        Assert.AreEqual(0, t.Size);
    }


    /// <summary>
    ///     Empty graph should contain nothing
    /// </summary>
    [TestMethod]
    public void EmptyEnumeratorTest()
    {
        var t = new DependencyGraph();
        t.AddDependency("x", "y");
        var e1 = t.GetDependees("y").GetEnumerator();
        Assert.IsTrue(e1.MoveNext());
        Assert.AreEqual("x", e1.Current);
        var e2 = t.GetDependents("x").GetEnumerator();
        Assert.IsTrue(e2.MoveNext());
        Assert.AreEqual("y", e2.Current);
        t.RemoveDependency("x", "y");
        Assert.IsFalse(t.GetDependees("y").GetEnumerator().MoveNext());
        Assert.IsFalse(t.GetDependents("x").GetEnumerator().MoveNext());
    }


    /// <summary>
    ///     Replace on an empty DG shouldn't fail
    /// </summary>
    [TestMethod]
    public void SimpleReplaceTest()
    {
        var t = new DependencyGraph();
        t.AddDependency("x", "y");
        Assert.AreEqual(t.Size, 1);
        t.RemoveDependency("x", "y");
        t.ReplaceDependents("x", new HashSet<string>());
        t.ReplaceDependees("y", new HashSet<string>());
    }


    /// <summary>
    ///     It should be possibe to have more than one DG at a time.
    /// </summary>
    [TestMethod]
    public void StaticTest()
    {
        var t1 = new DependencyGraph();
        var t2 = new DependencyGraph();
        t1.AddDependency("x", "y");
        Assert.AreEqual(1, t1.Size);
        Assert.AreEqual(0, t2.Size);
    }


    /// <summary>
    ///     Non-empty graph contains something
    /// </summary>
    [TestMethod]
    public void SizeTest()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "b");
        t.AddDependency("a", "c");
        t.AddDependency("c", "b");
        t.AddDependency("b", "d");
        Assert.AreEqual(4, t.Size);
    }


    /// <summary>
    ///     Non-empty graph contains something
    /// </summary>
    [TestMethod]
    public void EnumeratorTest()
    {
        var t = new DependencyGraph();
        t.AddDependency("a", "b");
        t.AddDependency("a", "c");
        t.AddDependency("c", "b");
        t.AddDependency("b", "d");

        var e = t.GetDependees("a").GetEnumerator();
        Assert.IsFalse(e.MoveNext());

        e = t.GetDependees("b").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        var s1 = e.Current;
        Assert.IsTrue(e.MoveNext());
        var s2 = e.Current;
        Assert.IsFalse(e.MoveNext());
        Assert.IsTrue((s1 == "a" && s2 == "c") || (s1 == "c" && s2 == "a"));

        e = t.GetDependees("c").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual("a", e.Current);
        Assert.IsFalse(e.MoveNext());

        e = t.GetDependees("d").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual("b", e.Current);
        Assert.IsFalse(e.MoveNext());
    }


    /// <summary>
    ///     Non-empty graph contains something
    /// </summary>
    [TestMethod]
    public void ReplaceThenEnumerate()
    {
        var t = new DependencyGraph();
        t.AddDependency("x", "b");
        t.AddDependency("a", "z");
        t.ReplaceDependents("b", new HashSet<string>());
        t.AddDependency("y", "b");
        t.ReplaceDependents("a", new HashSet<string> { "c" });
        t.AddDependency("w", "d");
        t.ReplaceDependees("b", new HashSet<string> { "a", "c" });
        t.ReplaceDependees("d", new HashSet<string> { "b" });

        var e = t.GetDependees("a").GetEnumerator();
        Assert.IsFalse(e.MoveNext());

        e = t.GetDependees("b").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        var s1 = e.Current;
        Assert.IsTrue(e.MoveNext());
        var s2 = e.Current;
        Assert.IsFalse(e.MoveNext());
        Assert.IsTrue((s1 == "a" && s2 == "c") || (s1 == "c" && s2 == "a"));

        e = t.GetDependees("c").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual("a", e.Current);
        Assert.IsFalse(e.MoveNext());

        e = t.GetDependees("d").GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual("b", e.Current);
        Assert.IsFalse(e.MoveNext());
    }


    /// <summary>
    ///     Using lots of data
    /// </summary>
    [TestMethod]
    public void StressTest()
    {
        // Dependency graph
        var t = new DependencyGraph();

        // A bunch of strings to use
        const int SIZE = 200;
        var letters = new string[SIZE];
        for (var i = 0; i < SIZE; i++) letters[i] = "" + (char)('a' + i);

        // The correct answers
        var dents = new HashSet<string>[SIZE];
        var dees = new HashSet<string>[SIZE];
        for (var i = 0; i < SIZE; i++)
        {
            dents[i] = new HashSet<string>();
            dees[i] = new HashSet<string>();
        }

        // Add a bunch of dependencies
        for (var i = 0; i < SIZE; i++)
        for (var j = i + 1; j < SIZE; j++)
        {
            t.AddDependency(letters[i], letters[j]);
            dents[i].Add(letters[j]);
            dees[j].Add(letters[i]);
        }

        // Remove a bunch of dependencies
        for (var i = 0; i < SIZE; i++)
        for (var j = i + 4; j < SIZE; j += 4)
        {
            t.RemoveDependency(letters[i], letters[j]);
            dents[i].Remove(letters[j]);
            dees[j].Remove(letters[i]);
        }

        // Add some back
        for (var i = 0; i < SIZE; i++)
        for (var j = i + 1; j < SIZE; j += 2)
        {
            t.AddDependency(letters[i], letters[j]);
            dents[i].Add(letters[j]);
            dees[j].Add(letters[i]);
        }

        // Remove some more
        for (var i = 0; i < SIZE; i += 2)
        for (var j = i + 3; j < SIZE; j += 3)
        {
            t.RemoveDependency(letters[i], letters[j]);
            dents[i].Remove(letters[j]);
            dees[j].Remove(letters[i]);
        }

        // Make sure everything is right
        for (var i = 0; i < SIZE; i++)
        {
            Assert.IsTrue(dents[i].SetEquals(new HashSet<string>(t.GetDependents(letters[i]))));
            Assert.IsTrue(dees[i].SetEquals(new HashSet<string>(t.GetDependees(letters[i]))));
        }
    }
}