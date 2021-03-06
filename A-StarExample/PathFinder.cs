﻿using System.Collections.Generic;

namespace A_StarExample
{
    public class PathFinder
    {
        private int width;
        private int height;
        private Node[,] nodes;
        private Node startNode;
        private Node endNode;
        private SearchParameters searchParameters;

        public PathFinder(SearchParameters searchParameters)
        {
            this.searchParameters = searchParameters;
            InitializeNodes(searchParameters.Map);
            startNode = nodes[searchParameters.StartLocation.X, searchParameters.StartLocation.Y];
            startNode.State = NodeState.Open;
            endNode = nodes[searchParameters.EndLocation.X, searchParameters.EndLocation.Y];
        }

        public List<Point> FindPath()
        {
            // The start node is the first entry in the 'open' list
            List<Point> path = new List<Point>();
            bool success = Search(startNode);
            if (success)
            {
                // If a path was found, follow the parents from the end node to build a list of locations
                Node node = endNode;
                while (node.Parent != null)
                {
                    path.Add(node.Location);
                    node = node.Parent;
                }

                // Reverse the list so it's in the correct order when returned
                path.Reverse();
            }

            return path;
        }

        private void InitializeNodes(bool[,] map)
        {
            width = map.GetLength(0);
            height = map.GetLength(1);
            nodes = new Node[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    nodes[x, y] = new Node(x, y, map[x, y], searchParameters.EndLocation);
                }
            }
        }

        private bool Search(Node currentNode)
        {
            // Set the current node to Closed since it cannot be traversed more than once
            currentNode.State = NodeState.Closed;
            List<Node> nextNodes = GetAdjacentWalkableNodes(currentNode);

            // Sort by F-value so that the shortest possible routes are considered first
            nextNodes.Sort((node1, node2) => node1.F.CompareTo(node2.F));
            foreach (var nextNode in nextNodes)
            {
                // Check whether the end node has been reached
                if (nextNode.Location == endNode.Location)
                {
                    return true;
                }
                else
                {
                    // If not, check the next set of nodes
                    if (Search(nextNode)) // Note: Recurses back into Search(Node)
                        return true;
                }
            }

            // The method returns false if this path leads to be a dead end
            return false;
        }

        /// <summary>
        /// Returns any nodes that are adjacent to <paramref name="fromNode"/> and may be considered to form the next step in the path
        /// </summary>
        /// <param name="fromNode">The node from which to return the next possible nodes in the path</param>
        /// <returns>A list of next possible nodes in the path</returns>
        private List<Node> GetAdjacentWalkableNodes(Node fromNode)
        {
            List<Node> walkableNodes = new List<Node>();
            IEnumerable<Point> nextLocations = GetAdjacentLocations(fromNode.Location);

            foreach (var location in nextLocations)
            {
                int x = location.X;
                int y = location.Y;

                // Stay within the grid's boundaries
                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;

                Node node = nodes[x, y];
                // Ignore non-walkable nodes
                if (!node.IsWalkable)
                    continue;

                // Ignore already-closed nodes
                if (node.State == NodeState.Closed)
                    continue;

                // Already-open nodes are only added to the list if their G-value is lower going via this route.
                if (node.State == NodeState.Open)
                {
                    float traversalCost = Node.GetTraversalCost(node.Location, node.Parent.Location);
                    float gTemp = fromNode.G + traversalCost;
                    if (gTemp < node.G)
                    {
                        node.Parent = fromNode;
                        walkableNodes.Add(node);
                    }
                }
                else
                {
                    // If it's untested, set the parent and flag it as 'Open' for consideration
                    node.Parent = fromNode;
                    node.State = NodeState.Open;
                    walkableNodes.Add(node);
                }
            }

            return walkableNodes;
        }

        /// <summary>
        /// Returns the eight locations immediately adjacent (orthogonally and diagonally) to <paramref name="fromLocation"/>
        /// </summary>
        /// <param name="fromLocation">The location from which to return all adjacent points</param>
        /// <returns>The locations as an IEnumerable of Points</returns>
        private static IEnumerable<Point> GetAdjacentLocations(Point fromLocation)
        {
            return new Point[]
            {
                new Point(fromLocation.X-1, fromLocation.Y-1),
                new Point(fromLocation.X-1, fromLocation.Y  ),
                new Point(fromLocation.X-1, fromLocation.Y+1),
                new Point(fromLocation.X,   fromLocation.Y+1),
                new Point(fromLocation.X+1, fromLocation.Y+1),
                new Point(fromLocation.X+1, fromLocation.Y  ),
                new Point(fromLocation.X+1, fromLocation.Y-1),
                new Point(fromLocation.X,   fromLocation.Y-1)
            };
        }
    }
}
