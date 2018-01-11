using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGamesAPI.Algorithms
{
    public class Graph<T>
    {
        public Dictionary<T, HashSet<T>> AdjacencyList { get; } = new Dictionary<T, HashSet<T>>();

        public Graph(IEnumerable<T> vertices, IEnumerable<Tuple<T, T>> edges)
        {
            foreach (var vertex in vertices ?? Enumerable.Empty<T>())
            {
                AddVertex(vertex);
            }

            foreach (var edge in edges ?? Enumerable.Empty<Tuple<T, T>>())
            {
                AddEdge(edge);
            }
        }

        private void AddEdge(Tuple<T, T> edge)
        {
            if (AdjacencyList.ContainsKey(edge.Item1) && AdjacencyList.ContainsKey(edge.Item2))
            {
                AdjacencyList[edge.Item1].Add(edge.Item2);
                AdjacencyList[edge.Item2].Add(edge.Item1);
            }
        }

        private void AddVertex(T vertex)
        {
            AdjacencyList[vertex] = new HashSet<T>();
        }
    }
}
