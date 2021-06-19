using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace PathPlaning
{
    [Serializable]
    public class NavigationGraph
    {
        private List<GraphNode> m_nodes;
        public List<GraphNode> nodes
        {
            get => m_nodes;
        }

        private List<GraphEdge> m_edges;
        public List<GraphEdge> edges
        {
            get => m_edges;
        }

        public NavigationGraph()
        {
            m_edges = new List<GraphEdge>();
            m_nodes = new List<GraphNode>();
        }

        #region Nodes
        public void AddNodes(params GraphNode[] nodes)
        {
            for (int i = 0; i < nodes.Length; ++i)
            {
                AddNode(nodes[i]);
            }
        }
        public void AddNode(GraphNode node)
        {
            if (!m_nodes.Contains(node))
            {
                m_nodes.Add(node);
            }
        }
        public void RemoveNode(GraphNode node)
        {
            if (!m_nodes.Contains(node))
            {
                m_nodes.Remove(node);
                
                // Remove all edges containing this node
                foreach(var edge in m_edges.Where((edge) => edge.ContainsNode(node)))
                {
                    RemoveEdge(edge);
                }
            }
        }

        public GraphNode GetClosestNode(Vector3 position)
        {
            var minDistSqr = float.MaxValue;
            GraphNode closestNode = null;
            foreach(var node in m_nodes)
            {
                var distSqr = (node.position - position).sqrMagnitude;
                if (minDistSqr > distSqr)
                {
                    minDistSqr = distSqr;
                    closestNode = node;
                }
            }

            return closestNode;
        }
        #endregion // Nodes

        #region  Edges
        public void AddEdge(GraphNode nodeA, GraphNode nodeB)
        {
            var edge = new GraphEdge(nodeA, nodeB);
            AddEdge(edge);
        }

        public void AddEdge(GraphEdge edge)
        {
            if (!m_edges.Contains(edge))
            {
                m_edges.Add(edge);

                // Add connections
                edge.nodeA.AddEdge(edge);
                edge.nodeB.AddEdge(edge);


            }
        }

        public void RemoveEdge(GraphEdge edge)
        {
            edge.nodeA.RemoveEdge(edge);
            edge.nodeB.RemoveEdge(edge);

            m_edges.Remove(edge);
        }
        #endregion // Edges
    }

    
    [Serializable]
    public class GraphNode : IEquatable<GraphNode>
    {
        private int m_id;
        public int ID
        {
            get => m_id;
        }

        private Vector3 m_position;
        public Vector3 position
        {
            get => m_position;
        }


        private HashSet<GraphEdge> m_neighbors;
        public HashSet<GraphEdge> neighbors
        {
            get => m_neighbors;
        }

        public GraphNode(int id, Vector3 position)
        {
            m_id = id;
            m_position = position;

            m_neighbors = new HashSet<GraphEdge>();
        }

        public void AddEdge(GraphEdge edge)
        {
            if (!m_neighbors.Contains(edge))
            {
                m_neighbors.Add(edge);
            }
        }

        public void RemoveEdge(GraphEdge edge)
        {
            if (m_neighbors.Contains(edge))
            {
                m_neighbors.Remove(edge);
            }
        }

        public override bool Equals(object obj) => obj is GraphNode other && this.Equals(other);

        public  bool Equals(GraphNode p) => this.m_id == p.m_id;

        public override int GetHashCode() => m_id.GetHashCode();

        public static bool operator ==(GraphNode lhs, GraphNode rhs) => lhs.Equals(rhs);

        public static bool operator !=(GraphNode lhs, GraphNode rhs) => !(lhs == rhs);
    }

    [Serializable]
    public class GraphEdge : IEquatable<GraphEdge>
    {
        private GraphNode[] m_nodes;
        public GraphNode nodeA
        {
            get => m_nodes[0];
        }
        public GraphNode nodeB
        {
            get => m_nodes[1];
        }

        private float m_weight;
        public float weight
        {
            get => m_weight;
        }

        public GraphEdge(GraphNode nodeA, GraphNode nodeB)
        {
            m_nodes = new GraphNode[2];
            m_weight = (nodeA.position - nodeB.position).magnitude;

            m_nodes[0] = nodeA;
            m_nodes[1] = nodeB;
        }

        public bool ContainsNode(GraphNode node)
        {
            return (nodeA == node || nodeB == node);
        }

        public override bool Equals(object obj) => obj is GraphEdge other && this.Equals(other);
        public bool Equals(GraphEdge edge) => this.nodeA == edge.nodeA && this.nodeB == edge.nodeB;
        public override int GetHashCode() => nodeA.GetHashCode() ^ nodeB.GetHashCode();
        public static bool operator ==(GraphEdge lhs, GraphEdge rhs) => lhs.Equals(rhs);
        public static bool operator !=(GraphEdge lhs, GraphEdge rhs) => !(lhs == rhs);
    }
}
