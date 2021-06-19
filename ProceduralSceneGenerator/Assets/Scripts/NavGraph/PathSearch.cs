using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathPlaning
{
    public static class PathSearch
    {
        #region  Classes
        public class PriorityQueue<T>
        {
            // В этом примере я использую несортированный массив, но в идеале
            // это должна быть двоичная куча. Существует открытый запрос на добавление
            // двоичной кучи к стандартной библиотеке C#: https://github.com/dotnet/corefx/issues/574
            //
            // Но пока её там нет, можно использовать класс двоичной кучи:
            // * https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
            // * http://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c.aspx
            // * http://xfleury.github.io/graphsearch.html
            // * http://stackoverflow.com/questions/102398/priority-queue-in-net
            
            private List<Tuple<T, double>> elements = new List<Tuple<T, double>>();

            public int Count
            {
                get { return elements.Count; }
            }
            
            public void Enqueue(T item, double priority)
            {
                elements.Add(Tuple.Create(item, priority));
            }

            public T Dequeue()
            {
                int bestIndex = 0;

                for (int i = 0; i < elements.Count; i++) {
                    if (elements[i].Item2 < elements[bestIndex].Item2) {
                        bestIndex = i;
                    }
                }

                T bestItem = elements[bestIndex].Item1;
                elements.RemoveAt(bestIndex);
                return bestItem;
            }
        }
        #endregion // Classes

        public delegate float HeuristicFunction(GraphNode currentNode, GraphNode targetNode);

        private static float DefaultHeuristic(GraphNode currentNode, GraphNode targetNode)
        {
            return (currentNode.position - targetNode.position).sqrMagnitude;
        }

        public static int[] DijkstraSearch(NavigationGraph graph, int starID, int targetID = -1)
        {
            List<GraphEdge> shortestPathTree = new List<GraphEdge>(graph.nodes.Count);
            List<float> costToTheNode = new List<float>(graph.nodes.Count);
            List<GraphEdge> searchFrontier = new List<GraphEdge>(graph.nodes.Count);
            PriorityQueue<int> pq = new PriorityQueue<int>();

            pq.Enqueue(starID, 0);
            while (pq.Count > 0)
            {
                int nextClosestNodeID = pq.Dequeue();

                shortestPathTree[nextClosestNodeID] = searchFrontier[nextClosestNodeID];

                if (nextClosestNodeID == targetID)
                {
                    return null;
                }

                var node = graph.nodes[nextClosestNodeID];
                foreach (var nodeN in node.neighbors)
                {
                    //float newCost = 
                }
            }
            
            return null;
        }

        public static int[] AStarSearch(NavigationGraph graph, int startID, int targetID, HeuristicFunction HeuristicFunction = null)
        {
            if (HeuristicFunction == null)
            {
                HeuristicFunction = DefaultHeuristic;
            }

            Dictionary<int, int> cameFrom = new Dictionary<int, int>();
            Dictionary<int, double> costSoFar = new Dictionary<int, double>();
        
            var frontier = new PriorityQueue<int>();
            frontier.Enqueue(startID, 0);

            cameFrom[startID] = startID;
            costSoFar[startID] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current.Equals(targetID))
                {
                    break;
                }

                var node = graph.nodes[current];
                foreach (var outComeEdge in node.neighbors)
                {
                    var otherNodeID = outComeEdge.nodeA.ID != current ? outComeEdge.nodeA.ID : outComeEdge.nodeB.ID;
                    double newCost = costSoFar[current] + outComeEdge.weight;

                    if (!costSoFar.ContainsKey(otherNodeID) || newCost < costSoFar[otherNodeID])
                    {
                        costSoFar[otherNodeID] = newCost;
                        double priority = newCost + HeuristicFunction(graph.nodes[otherNodeID], graph.nodes[targetID]);
                        frontier.Enqueue(otherNodeID, priority);
                        cameFrom[otherNodeID] = current;
                    }
                }
            }

            // Extract path
            int nodeID = targetID;
            int cameFromNodeID = cameFrom[nodeID];
            var path = new Stack<int>();
            path.Push(nodeID);
            path.Push(cameFromNodeID);
            while(cameFromNodeID != nodeID)
            {
                nodeID = cameFromNodeID;
                cameFromNodeID = cameFrom[nodeID];
                path.Push(cameFromNodeID);
            }

            return path.ToArray();
        }
    }
}
