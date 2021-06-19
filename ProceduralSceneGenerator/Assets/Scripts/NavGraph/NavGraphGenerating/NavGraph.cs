using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PathPlaning;

public class NavGraph : MonoBehaviour
{
    [SerializeField]
    private bool m_drawGraph = true;
    [SerializeField]
    private bool m_drawPath = false;

    [SerializeField]
    private float m_agentRadius = 1f;

    [SerializeField]
    private GameObject m_pointsContainer;
    [SerializeField]
    private GameObject m_pathContainer;
    [SerializeField]
    private GraphAsset m_graphAsset;

    private NavigationGraph m_graph;

    private List<Vector3> m_lastPath = new List<Vector3>();

    public void BakeNavGraph()
    {
        if (m_pointsContainer == null || m_pointsContainer.transform.childCount == 0)
        {
            Debug.LogError("Error while backing NavGraph. Points container is null or empty");
            return;
        }

        var pointsCount = m_pointsContainer.transform.childCount;

        var points = m_pointsContainer.GetComponentsInChildren<PointOfVisibilityPlacer>(false);
        foreach(var point in points)
        {
            point.radius = m_agentRadius;
        }

        m_graph = new PathPlaning.NavigationGraph();
        
        // Generate graph nodes
        m_graph.AddNodes(GenerateNodes(points));
        // Generate edges between all nodes
        foreach(var nodeA in m_graph.nodes)
        {
            foreach(var nodeB in m_graph.nodes)
            {
                if (nodeA != nodeB)
                {
                    m_graph.AddEdge(nodeA, nodeB);
                }
            }
        }

        // Validate all edges
        // Physics.Raycast(edge.nodeA.position, edge.nodeB.position - edge.nodeA.position, edge.weight)
        // Physics.SphereCast(new Ray(edge.nodeA.position,  edge.nodeB.position - edge.nodeA.position), m_agentRadius, edge.weight)
        var invalidEdges = m_graph.edges.FindAll((edge) =>
            {
                var offset = Vector3.Cross((edge.nodeB.position - edge.nodeA.position), Vector3.up).normalized * m_agentRadius / 2f;
                // centre
                bool hit = Physics.Raycast(edge.nodeA.position,  edge.nodeB.position - edge.nodeA.position, edge.weight);
                // left
                hit = hit || Physics.Raycast(edge.nodeA.position + offset,  edge.nodeB.position - edge.nodeA.position, edge.weight);
                // right
                hit = hit || Physics.Raycast(edge.nodeA.position - offset,  edge.nodeB.position - edge.nodeA.position,  edge.weight);

                return hit;
            }
        );
        foreach(var edge in invalidEdges)
        {
            m_graph.RemoveEdge(edge);
        }

        m_graphAsset = GraphAsset.SaveGraphToAsset(m_graph, m_graph.GetHashCode().ToString().Remove(7));
    }

    internal void OverrideVisibilityPointsRadius(float radius = -1)
    {
        if (radius == -1)
        {
            radius = this.m_agentRadius;
        }
        
        var points = m_pointsContainer.GetComponentsInChildren<PointOfVisibilityPlacer>(false);
        foreach(var point in points)
        {
            point.radius = radius;
        }
    }

    private GraphNode[] GenerateNodes(PointOfVisibilityPlacer[] placers)
    {
        var graphNodes = new GraphNode[placers.Length];
        for (int i = 0; i < placers.Length; ++i)
        {
            graphNodes[i] = new GraphNode(i, placers[i].position);
        }

        return graphNodes;
    }


    public void BuildPath()
    {
        if (m_graph == null)
        {
            return;
        }

        var wayPoints = m_pathContainer.GetComponentsInChildren<WaypointPlacer>();
        var startNode = m_graph.GetClosestNode(wayPoints[0].position);
        var targetNode = m_graph.GetClosestNode(wayPoints[1].position);

        var pathNodes = PathSearch.AStarSearch(m_graph, startNode.ID, targetNode.ID);

        m_lastPath.Clear();
        m_lastPath.Add(wayPoints[0].position);
        for (int i = 0; i < pathNodes.Length; ++i)
        {
            int nodeID = pathNodes[i];
            m_lastPath.Add(m_graph.nodes[nodeID].position);
        }
        m_lastPath.Add(wayPoints[1].position);
    }

    private void OnDrawGizmos()
    {
        var graph = m_graph != null ? m_graph : m_graphAsset != null ? m_graphAsset.navigationGraph : null;
        if (m_drawGraph && graph != null)
        {
            var color = Color.gray;
            color.a = 0.25f;
            Gizmos.color = color;
            foreach(var edge in graph.edges)
            {
                //var offset = Vector3.Cross((edge.nodeB.position - edge.nodeA.position), Vector3.up).normalized * m_agentRadius / 2f;
                // left
                //Gizmos.DrawLine(edge.nodeA.position + offset, edge.nodeB.position + offset);
                // right
                //Gizmos.DrawLine(edge.nodeA.position - offset, edge.nodeB.position - offset);
                Gizmos.DrawLine(edge.nodeA.position, edge.nodeB.position);
            }
        }

        if (m_drawPath && m_lastPath != null && m_lastPath.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < m_lastPath.Count - 1; ++i)
            {
                Gizmos.DrawLine(m_lastPath[i], m_lastPath[i + 1]);
            }
        }
    }
}

[CustomEditor(typeof(NavGraph))]
public class NavGraphEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var navGraph = target as NavGraph;

        if (GUILayout.Button("Bake NavGrap") && navGraph)
        {
            navGraph.BakeNavGraph();
        }
        if (GUILayout.Button("Override POV radius") && navGraph)
        {
            navGraph.OverrideVisibilityPointsRadius();
        }
        if (GUILayout.Button("Build path") && navGraph)
        {
            navGraph.BuildPath();
        }
    }
}