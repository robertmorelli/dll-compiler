// Skeleton implementation written by Joe Zachary for CS 3500, September 2013.
// Version 1.1 (Fixed error in comment for RemoveDependency.)
// Version 1.2 - Daniel Kopta 
//               (Clarified meaning of dependent and dependee.)
//               (Clarified names in solution/project structure.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpreadsheetUtilities
{

    /// <summary>
    /// (s1,t1) is an ordered pair of strings
    /// t1 depends on s1; s1 must be evaluated before t1
    /// 
    /// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
    /// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
    /// Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
    /// set, and the element is already in the set, the set remains unchanged.
    /// 
    /// Given a DependencyGraph DG:
    /// 
    ///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
    ///        (The set of things that depend on s)    
    ///        
    ///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
    ///        (The set of things that s depends on) 
    //
    // For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
    //     dependents("a") = {"b", "c"}
    //     dependents("b") = {"d"}
    //     dependents("c") = {}
    //     dependents("d") = {"d"}
    //     dependees("a") = {}
    //     dependees("b") = {"a"}
    //     dependees("c") = {"a"}
    //     dependees("d") = {"b", "d"}
    /// </summary>
    public class DependencyGraph
    {
        private readonly Dictionary<string, HashSet<string>> dependeeSets = [];
        private readonly Dictionary<string, HashSet<string>> dependantSets = [];
        /// <summary>
        /// Creates an empty DependencyGraph.
        /// </summary>
        public DependencyGraph()
        {
        }


        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>
        public int Size
        {
            get { return dependantSets.Values.Select(s => s.Count).Sum(); }
        }


        /// <summary>
        /// The size of dependees(s).
        /// This property is an example of an indexer.  If dg is a DependencyGraph, you would
        /// invoke it like this:
        /// dg["a"]
        /// It should return the size of dependees("a")
        /// </summary>
        public int this[string s]
        {
            get { return dependeeSets.ContainsKey(s) ? dependeeSets[s].Count : 0; }
        }


        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        public bool HasDependents(string s)
        {
            return dependantSets.ContainsKey(s) ? dependantSets[s].Count > 0 : false;
        }


        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        public bool HasDependees(string s)
        {
            return dependeeSets.ContainsKey(s) ? dependeeSets[s].Count > 0 : false;
        }


        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        public IEnumerable<string> GetDependents(string s)
        {
            return (dependantSets.ContainsKey(s) ? dependantSets[s] : []).AsEnumerable();
        }

        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        public IEnumerable<string> GetDependees(string s)
        {
            return (dependeeSets.ContainsKey(s) ? dependeeSets[s] : []).AsEnumerable();
        }


        /// <summary>
        /// <para>Adds the ordered pair (s,t), if it doesn't exist</para>
        /// 
        /// <para>This should be thought of as:</para>   
        /// 
        ///   t depends on s
        ///
        /// </summary>
        /// <param name="s"> s must be evaluated first. T depends on S</param>
        /// <param name="t"> t cannot be evaluated until s is</param>        /// 
        public void AddDependency(string s, string t)
        {
            if (dependantSets.ContainsKey(s))
            {
                dependantSets[s].Add(t);
            } else {
                dependantSets[s] = [t];
            }

            if (dependeeSets.ContainsKey(t))
            {
                dependeeSets[t].Add(s);
            }
            else
            {
                dependeeSets[t] = [s];
            }
            
        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void RemoveDependency(string s, string t)
        {
            if (dependantSets.ContainsKey(s))
            {
                if (dependantSets[s].Contains(t))
                {
                    dependantSets[s].Remove(t);
                    if (dependantSets[s].Count == 0)
                    {
                        dependantSets.Remove(s);
                    }
                }
            }
            if (dependeeSets.ContainsKey(t))
            {
                if (dependeeSets[t].Contains(s))
                {
                    dependeeSets[t].Remove(s);
                    if (dependeeSets[t].Count == 0)
                    {
                        dependeeSets.Remove(t);
                    }
                }
            }
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {
            if (dependantSets.ContainsKey(s))
            {
                foreach (string t in dependantSets[s])
                {
                    RemoveDependency(s, t);
                }
            }
            foreach (string t in newDependents)
            {
                AddDependency(s, t);
            }
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {
            if (dependeeSets.ContainsKey(s))
            {
                foreach (string t in dependeeSets[s])
                {
                    RemoveDependency(t, s);
                }
            }
            foreach (string t in newDependees)
            {
                AddDependency(t,s);
            }
        }

        private void printAll() {
            Console.Write("ant");
            foreach (string s in dependantSets.Keys)
            {
                Console.Write("\n:: " + s + " | ");
                foreach (string t in dependantSets[s])
                {
                    Console.Write(t);
                }
            }
            Console.Write("\nees");
            foreach (string s in dependeeSets.Keys)
            {
                Console.Write("\n:: " + s + " | ");
                foreach (string t in dependeeSets[s])
                {
                    Console.Write(t);
                }
            }
            Console.WriteLine();
        }

    }

}
