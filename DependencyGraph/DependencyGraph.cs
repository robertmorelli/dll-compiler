/// <summary>
/// Worst Implementation of the dependency graph assignment
/// Version 4
/// Built by Robert Morelli for 3500
/// solution -> store everything twice so lookup is trivial
/// I, Robert Morelli, certify that I wrote this code from scratch and
/// did not copy it in part or whole from code that is not my own. All 
/// references to code that is not my own used in the completion of the
/// assignments are cited in my README file.
/// </summary>s

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
        //store both directions for easy access "2n = n" in software after all
        private readonly Dictionary<string, HashSet<string>> dependeeSets = [];
        private readonly Dictionary<string, HashSet<string>> dependantSets = [];//yes they are ants (not typo)
        /// <summary>
        /// Creates an empty DependencyGraph.
        /// </summary>
        public DependencyGraph() { }


        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>
        public int Size
        {
            //get all the items. then count whats inside for each. then sum the contents
            get => dependantSets.Values.Select(s => s.Count).Sum();
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
            //if we have it return the count. if we dont i guess its zero
            get => dependeeSets.ContainsKey(s) ? dependeeSets[s].Count : 0;
        }


        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        public bool HasDependents(string s) => dependantSets.ContainsKey(s);

        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        public bool HasDependees(string s) => dependeeSets.ContainsKey(s);

        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        public IEnumerable<string> GetDependents(string s)
        {
            //pretty self explanitory
            return dependantSets.ContainsKey(s) ? dependantSets[s] : [];
        }

        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        public IEnumerable<string> GetDependees(string s)
        {
            //pretty self explanitory
            return dependeeSets.ContainsKey(s) ? dependeeSets[s] : [];
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
            //if it aint there add a spot. then add the thing in the spot we may or may not have made
            if (!dependantSets.ContainsKey(s)) dependantSets[s] = [];
            dependantSets[s].Add(t);
            if (!dependeeSets.ContainsKey(t)) dependeeSets[t] = [];
            dependeeSets[t].Add(s);
        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void RemoveDependency(string s, string t)
        {
            //for simplicity we gaurentee it exists
            AddDependency(s, t);
            //remove it then remove our spot for it if its not needed anymore
            dependantSets[s].Remove(t);
            if (dependantSets[s].Count == 0) dependantSets.Remove(s);
            dependeeSets[t].Remove(s);
            if (dependeeSets[t].Count == 0) dependeeSets.Remove(t);
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {
            //gaurentee existence. delete everything in it (symetry gaurenteed by RemoveDependency)
            //add everything new
            //delete if nothing new
            if (!dependantSets.ContainsKey(s)) dependantSets[s] = [];
            foreach (var t in dependantSets[s]) RemoveDependency(s, t);
            foreach (var t in newDependents) AddDependency(s, t);
            if (dependantSets.ContainsKey(s)) if (this[s] == 0) dependantSets.Remove(s);
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {
            //gaurentee existence. delete everything in it (symetry gaurenteed by RemoveDependency)
            //add everything new
            //delete if nothing new
            if (!dependeeSets.ContainsKey(s)) dependeeSets[s] = [];
            foreach (var t in dependeeSets[s]) RemoveDependency(t, s);
            foreach (var t in newDependees) AddDependency(t, s);
            if (dependeeSets.ContainsKey(s)) if (this[s] == 0) dependeeSets.Remove(s);
        }
    }

}
