using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;

namespace DevelopmentTests
{
    /// <summary>
    ///This is a test class for DependencyGraphTest and is intended
    ///to contain all DependencyGraphTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DependencyGraphTests
    {
        /// <summary>
        /// General coverage case
        /// </summary>
        [TestMethod()]
        public void DependencyGraphCoverageTest()
        {
            DependencyGraph t = new DependencyGraph();

            //check every single method with and without state in our obj
            Assert.AreEqual(0, t.Size);
            Assert.AreEqual(0, t["a"]);
            Assert.AreEqual(false, t.HasDependents("a"));
            Assert.AreEqual(false, t.HasDependees("a"));
            IEnumerable<string> t2 = t.GetDependents("a");
            Assert.AreEqual(0, t2.Count());
            t2 = t.GetDependees("a");
            Assert.AreEqual(0, t2.Count());
            t.RemoveDependency("a", "a");
            Assert.AreEqual(0, t["a"]);
            t.ReplaceDependents("a", []);
            Assert.AreEqual(0, t["a"]);
            t.ReplaceDependees("a", []);
            Assert.AreEqual(0, t["a"]);

            t.AddDependency("a", "a");

            Assert.AreEqual(1, t.Size);
            Assert.AreEqual(1, t["a"]);
            Assert.AreEqual(true, t.HasDependents("a"));
            Assert.AreEqual(true, t.HasDependees("a"));
            t2 = t.GetDependents("a");
            Assert.AreEqual(1, t2.Count());
            t2 = t.GetDependees("a");
            Assert.AreEqual(1, t2.Count());
            t.RemoveDependency("a", "a");
            Assert.AreEqual(0, t["a"]);
            t.AddDependency("a", "a");
            t.ReplaceDependents("a", ["a"]);
            Assert.AreEqual(1, t["a"]);
            t.ReplaceDependees("a", ["a"]);
            Assert.AreEqual(1, t["a"]);
        }

        /// <summary>
        ///check that adding and removing a lot of elements doesnt fuck the machine
        ///</summary>
        [TestMethod()]
        public void MemLeakTest()
        {

            //preallocated stuff
            const int SIZE = 20;
            string[] As = new string[SIZE];
            string[] Bs = new string[SIZE];
            for (int i = 0; i < SIZE; i++)
            {
                As[i] = "" + (char)('a' + i);
                Bs[i] = "" + (char)('b' + i);
            }


            DependencyGraph t = new DependencyGraph();
            int preventOptimizations = 0;


            //add and remove stuff

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < SIZE; i++)
                {
                    t.AddDependency(As[i], Bs[i]);
                    preventOptimizations += t.Size;
                }
                for (int i = 0; i < SIZE; i++)
                {
                    t.RemoveDependency(As[i], Bs[i]);
                    preventOptimizations += t.Size;
                }
            }

            //add stuff to bias the test (its semi-random so this is necessary)
            for (int i = 0; i < SIZE; i++)
            {
                t.AddDependency(As[i], Bs[i]);
                preventOptimizations += t.Size;
            }

            long memStart = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;

            //do the same stuff
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < SIZE; i++)
                {
                    t.AddDependency(As[i], Bs[i]);
                    preventOptimizations += t.Size;
                }
                for (int i = 0; i < SIZE; i++)
                {
                    t.RemoveDependency(As[i], Bs[i]);
                    preventOptimizations += t.Size;
                }
            }

            //check that we didnt increase the memory by an unreasonable amount
            long memEnd = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;
            Assert.AreEqual(true, (memEnd - memStart + 10000) < 0);
        }

        /// <summary>
        ///Empty graph should contain nothing
        ///</summary>
        [TestMethod()]
        public void SimpleEmptyTest()
        {
            DependencyGraph t = new DependencyGraph();
            Assert.AreEqual(0, t.Size);
        }


        /// <summary>
        ///Empty graph should contain nothing
        ///</summary>
        [TestMethod()]
        public void SimpleEmptyRemoveTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "y");
            Assert.AreEqual(1, t.Size);
            t.RemoveDependency("x", "y");
            Assert.AreEqual(0, t.Size);
        }


        /// <summary>
        ///Empty graph should contain nothing
        ///</summary>
        [TestMethod()]
        public void EmptyEnumeratorTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "y");
            IEnumerator<string> e1 = t.GetDependees("y").GetEnumerator();
            Assert.IsTrue(e1.MoveNext());
            Assert.AreEqual("x", e1.Current);
            IEnumerator<string> e2 = t.GetDependents("x").GetEnumerator();
            Assert.IsTrue(e2.MoveNext());
            Assert.AreEqual("y", e2.Current);
            t.RemoveDependency("x", "y");
            Assert.IsFalse(t.GetDependees("y").GetEnumerator().MoveNext());
            Assert.IsFalse(t.GetDependents("x").GetEnumerator().MoveNext());
        }


        /// <summary>
        ///Replace on an empty DG shouldn't fail
        ///</summary>
        [TestMethod()]
        public void SimpleReplaceTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "y");
            Assert.AreEqual(t.Size, 1);
            t.RemoveDependency("x", "y");
            t.ReplaceDependents("x", new HashSet<string>());
            t.ReplaceDependees("y", new HashSet<string>());
        }



        ///<summary>
        ///It should be possibe to have more than one DG at a time.
        ///</summary>
        [TestMethod()]
        public void StaticTest()
        {
            DependencyGraph t1 = new DependencyGraph();
            DependencyGraph t2 = new DependencyGraph();
            t1.AddDependency("x", "y");
            Assert.AreEqual(1, t1.Size);
            Assert.AreEqual(0, t2.Size);
        }




        /// <summary>
        ///Non-empty graph contains something
        ///</summary>
        [TestMethod()]
        public void SizeTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");
            Assert.AreEqual(4, t.Size);
        }


        /// <summary>
        ///Non-empty graph contains something
        ///</summary>
        [TestMethod()]
        public void EnumeratorTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");

            IEnumerator<string> e = t.GetDependees("a").GetEnumerator();
            Assert.IsFalse(e.MoveNext());

            e = t.GetDependees("b").GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            String s1 = e.Current;
            Assert.IsTrue(e.MoveNext());
            String s2 = e.Current;
            Assert.IsFalse(e.MoveNext());
            Assert.IsTrue(((s1 == "a") && (s2 == "c")) || ((s1 == "c") && (s2 == "a")));

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
        ///Non-empty graph contains something
        ///</summary>
        [TestMethod()]
        public void ReplaceThenEnumerate()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "b");
            t.AddDependency("a", "z");
            t.ReplaceDependents("b", new HashSet<string>());
            t.AddDependency("y", "b");
            t.ReplaceDependents("a", new HashSet<string>() { "c" });
            t.AddDependency("w", "d");
            t.ReplaceDependees("b", new HashSet<string>() { "a", "c" });
            t.ReplaceDependees("d", new HashSet<string>() { "b" });

            IEnumerator<string> e = t.GetDependees("a").GetEnumerator();
            Assert.IsFalse(e.MoveNext());

            e = t.GetDependees("b").GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            String s1 = e.Current;
            Assert.IsTrue(e.MoveNext());
            String s2 = e.Current;
            Assert.IsFalse(e.MoveNext());
            Assert.IsTrue(((s1 == "a") && (s2 == "c")) || ((s1 == "c") && (s2 == "a")));

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
        ///Using lots of data
        ///</summary>
        [TestMethod()]
        public void StressTest()
        {
            // Dependency graph
            DependencyGraph t = new DependencyGraph();

            // A bunch of strings to use
            const int SIZE = 200;
            string[] letters = new string[SIZE];
            for (int i = 0; i < SIZE; i++)
            {
                letters[i] = ("" + (char)('a' + i));
            }

            // The correct answers
            HashSet<string>[] dents = new HashSet<string>[SIZE];
            HashSet<string>[] dees = new HashSet<string>[SIZE];
            for (int i = 0; i < SIZE; i++)
            {
                dents[i] = new HashSet<string>();
                dees[i] = new HashSet<string>();
            }

            // Add a bunch of dependencies
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = i + 1; j < SIZE; j++)
                {
                    t.AddDependency(letters[i], letters[j]);
                    dents[i].Add(letters[j]);
                    dees[j].Add(letters[i]);
                }
            }

            // Remove a bunch of dependencies
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = i + 4; j < SIZE; j += 4)
                {
                    t.RemoveDependency(letters[i], letters[j]);
                    dents[i].Remove(letters[j]);
                    dees[j].Remove(letters[i]);
                }
            }

            // Add some back
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = i + 1; j < SIZE; j += 2)
                {
                    t.AddDependency(letters[i], letters[j]);
                    dents[i].Add(letters[j]);
                    dees[j].Add(letters[i]);
                }
            }

            // Remove some more
            for (int i = 0; i < SIZE; i += 2)
            {
                for (int j = i + 3; j < SIZE; j += 3)
                {
                    t.RemoveDependency(letters[i], letters[j]);
                    dents[i].Remove(letters[j]);
                    dees[j].Remove(letters[i]);
                }
            }

            // Make sure everything is right
            for (int i = 0; i < SIZE; i++)
            {
                Assert.IsTrue(dents[i].SetEquals(new HashSet<string>(t.GetDependents(letters[i]))));
                Assert.IsTrue(dees[i].SetEquals(new HashSet<string>(t.GetDependees(letters[i]))));
            }
        }

    }
}
