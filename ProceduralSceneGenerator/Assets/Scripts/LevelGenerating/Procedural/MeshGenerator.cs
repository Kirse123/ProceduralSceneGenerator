using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    [SerializeField]
    private bool m_drawGizmos = false;
    [SerializeField]
    private MeshFilter m_wallMeshFilter;
    public MeshFilter wallMeshFilter
    {
        get => m_wallMeshFilter;
    }

    [SerializeField]
    private MeshFilter m_meshFilter;
    public MeshFilter meshFilter
    {
        get => m_meshFilter;
    }

    [SerializeField]
    private MeshCollider m_wallCollider;
    [SerializeField]
    private PointOfVisibilityPlacer m_povPrefab;
    [SerializeField]
    private GameObject m_povContainer;

    private List<Vector3> m_verticies = new List<Vector3>();
    private List<int> m_triangles = new List<int>();

    private Dictionary<int, List<Triangle>> m_triangleDict = new Dictionary<int, List<Triangle>>();
    private List<List<int>> m_outlines = new List<List<int>>();
    private HashSet<int> m_checkedVertexes = new HashSet<int>();
    
    private class SqareGrid
    {
        private Square[,] m_squares;
        public Square[,] squares
        {
            get => m_squares;
        }

        public int width
        {
            get
            {
                if (m_squares != null)
                {
                    return m_squares.GetLength(0);
                }

                return 0;
            }
        }

        public int height
        {
            get
            {
                if (m_squares != null)
                {
                    return m_squares.GetLength(1);
                }

                return 0;
            }
        }
        
        public SqareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);

            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];
            for (int x = 0; x < nodeCountX; ++x)
            {
                for (int y = 0; y < nodeCountY; ++y)
                {
                    Vector3 pos = new Vector3(-mapWidth / 2f + x * squareSize + squareSize / 2f, 0, -mapHeight / 2f + y * squareSize + squareSize / 2f);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            m_squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; ++x)
            {
                for (int y = 0; y < nodeCountY - 1; ++y)
                {
                    m_squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }

    }

    private class Square
    {
        public ControlNode m_topRight, m_topLeft, m_bottomRight, m_bottomLeft;

        public Node m_centerBottom, m_centerTop, m_centerLeft, m_centerRight;
        public int configuratoin = 0;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft)
        {
            m_topRight = topRight;
            m_topLeft = topLeft;
            m_bottomLeft = bottomLeft;
            m_bottomRight = bottomRight;
            
            m_centerBottom = m_bottomLeft.rightNode;
            m_centerTop = m_topLeft.rightNode;
            m_centerLeft = m_bottomLeft.aboveNode;
            m_centerRight = m_bottomRight.aboveNode;


            if (m_topLeft.isActive)
            {
                configuratoin += 8;
            }
            if (m_topRight.isActive)
            {
                configuratoin += 4;
            }
            if (m_bottomRight.isActive)
            {
                configuratoin += 2;
            }
            if (m_bottomLeft.isActive)
            {
                configuratoin += 1;
            }
        }
    }
    
    private class ControlNode : Node
    {
        private bool m_isActive;
        public bool isActive
        {
            get => m_isActive;
            set => m_isActive = value;
        }

        private Node m_aboveNode;
        public Node aboveNode
        {
            get => m_aboveNode;
        }

        private Node m_rightNode;
        public Node rightNode
        {
            get => m_rightNode;
        }

        public ControlNode(Vector3 position, bool isActive, float squareSize) : base(position)
        {
            m_isActive = isActive;

            m_aboveNode = new Node(m_position + Vector3.forward * squareSize / 2f);
            m_rightNode = new Node(m_position + Vector3.right * squareSize / 2f);
        }
    }
    
    private class Node
    {
        protected Vector3 m_position;
        public Vector3 position
        {
            get => m_position;
        }

        public int vertexID = -1;

        public Node(Vector3 position)
        {
            m_position = position;
        }
    }
    
    private struct Triangle
    {
        public int vertexA;
        public int vertexB;
        public int vertexC;

        public Triangle(int vertA, int vertB, int vertC)
        {
            this.vertexA = vertA;
            this.vertexB = vertB;
            this.vertexC = vertC;
        }

        public bool ContainsVertex(int vertID)
        {
            return vertexA == vertID || vertexB == vertID || vertexC == vertID;
        }

        public int this[int i]
        {
            get
            {
                switch(i)
                {
                    case 0:
                        return vertexA;
                    case 1:
                        return vertexB;
                    case 2:
                        return vertexC;
                    default:
                        return -1;
                }
            }
        }
    }

    private SqareGrid m_sqareGrid;

    public void GenerateMesh(int[,] map, float squareSize)
    {
        m_sqareGrid = new SqareGrid(map, squareSize);
        m_verticies = new List<Vector3>();
        m_triangles = new List<int>();
        m_triangleDict = new Dictionary<int, List<Triangle>>();
        m_checkedVertexes.Clear();
        m_outlines.Clear();

        for (int x = 0; x < m_sqareGrid.squares.GetLength(0); ++x)
        {
            for (int y = 0; y < m_sqareGrid.squares.GetLength(1); ++y)
            {
                TriangulateSquare(m_sqareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        m_meshFilter = GetComponent<MeshFilter>();

        m_meshFilter.mesh = mesh;

        mesh.vertices = m_verticies.ToArray();
        mesh.triangles = m_triangles.ToArray();
        mesh.RecalculateNormals();

        CreateWallMesh();
    }

	private void TriangulateSquare(Square square)
    {
		switch (square.configuratoin) {
		case 0:
			break;

		// 1 points:
		case 1:
			MeshFromPoints(square.m_centerLeft, square.m_centerBottom, square.m_bottomLeft);
			break;
		case 2:
			MeshFromPoints(square.m_bottomRight, square.m_centerBottom, square.m_centerRight);
			break;
		case 4:
			MeshFromPoints(square.m_topRight, square.m_centerRight, square.m_centerTop);
			break;
		case 8:
			MeshFromPoints(square.m_topLeft, square.m_centerTop, square.m_centerLeft);
			break;

		// 2 points:
		case 3:
			MeshFromPoints(square.m_centerRight, square.m_bottomRight, square.m_bottomLeft, square.m_centerLeft);
			break;
		case 6:
			MeshFromPoints(square.m_centerTop, square.m_topRight, square.m_bottomRight, square.m_centerBottom);
			break;
		case 9:
			MeshFromPoints(square.m_topLeft, square.m_centerTop, square.m_centerBottom, square.m_bottomLeft);
			break;
		case 12:
			MeshFromPoints(square.m_topLeft, square.m_topRight, square.m_centerRight, square.m_centerLeft);
			break;
		case 5:
			MeshFromPoints(square.m_centerTop, square.m_topRight, square.m_centerRight, square.m_centerBottom, square.m_bottomLeft, square.m_centerLeft);
			break;
		case 10:
			MeshFromPoints(square.m_topLeft, square.m_centerTop, square.m_centerRight, square.m_bottomRight, square.m_centerBottom, square.m_centerLeft);
			break;

		// 3 point:
		case 7:
			MeshFromPoints(square.m_centerTop, square.m_topRight, square.m_bottomRight, square.m_bottomLeft, square.m_centerLeft);
			break;
		case 11:
			MeshFromPoints(square.m_topLeft, square.m_centerTop, square.m_centerRight, square.m_bottomRight, square.m_bottomLeft);
			break;
		case 13:
			MeshFromPoints(square.m_topLeft, square.m_topRight, square.m_centerRight, square.m_centerBottom, square.m_bottomLeft);
			break;
		case 14:
			MeshFromPoints(square.m_topLeft, square.m_topRight, square.m_bottomRight, square.m_centerBottom, square.m_centerLeft);
			break;

		// 4 point:
		case 15:
			MeshFromPoints(square.m_topLeft, square.m_topRight, square.m_bottomRight, square.m_bottomLeft);
            m_checkedVertexes.Add(square.m_topLeft.vertexID);
            m_checkedVertexes.Add(square.m_topRight.vertexID);
            m_checkedVertexes.Add(square.m_bottomRight.vertexID);
            m_checkedVertexes.Add(square.m_bottomLeft.vertexID);
			break;
		}
    }

    private void MeshFromPoints(params Node[] points)
    {
        AssignVericies(points);

		
        if (points.Length >= 3)
			CreateTriangle(points[0], points[1], points[2]);
		if (points.Length >= 4)
			CreateTriangle(points[0], points[2], points[3]);
		if (points.Length >= 5) 
			CreateTriangle(points[0], points[3], points[4]);
		if (points.Length >= 6)
			CreateTriangle(points[0], points[4], points[5]);
    }

    private void AssignVericies(params Node[] ponts)
    {
        for (int i = 0; i < ponts.Length; ++i)
        {
            if (ponts[i].vertexID == -1)
            {
                ponts[i].vertexID = m_verticies.Count;
                m_verticies.Add(ponts[i].position);
            }
        }
    }

    private void CreateTriangle(Node a, Node b, Node c)
    {
        m_triangles.Add(a.vertexID);
        m_triangles.Add(b.vertexID);
        m_triangles.Add(c.vertexID);

        var triangle = new Triangle(a.vertexID, b.vertexID, c.vertexID);
        AddTriangleToDict(triangle.vertexA, triangle);
        AddTriangleToDict(triangle.vertexB, triangle);
        AddTriangleToDict(triangle.vertexC, triangle);
    }

    private void AddTriangleToDict(int vertexId, Triangle triangle)
    {
        if (m_triangleDict.TryGetValue(vertexId, out var trianglesWithVertex))
        {
            trianglesWithVertex.Add(triangle);
        }
        else
        {
            m_triangleDict.Add(vertexId, new List<Triangle>{triangle});
        }
    }

    private void CalculateMeshOutlines()
    {
        for (int vertexID = 0; vertexID < m_verticies.Count; ++ vertexID)
        {
            if (!m_checkedVertexes.Contains(vertexID))
            {
                int newOutlineVertex = GetConnectedOutlineVertexID(vertexID);
                if (newOutlineVertex != -1)
                {
                    m_checkedVertexes.Add(vertexID);

                    var newOutline = new List<int>();
                    newOutline.Add(vertexID);
                    m_outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, m_outlines.Count - 1);
                    m_outlines[m_outlines.Count - 1].Add(vertexID);
                }
            }
        }
    }

    private void FollowOutline(int vertexID, int outlineID)
    {
        m_outlines[outlineID].Add(vertexID);
        m_checkedVertexes.Add(vertexID);
        int newOutlineVertex = GetConnectedOutlineVertexID(vertexID);
        
        if (newOutlineVertex != -1)
        {
            FollowOutline(newOutlineVertex, outlineID);
        }
    }

    private int GetConnectedOutlineVertexID(int vertexA)
    {
        if (m_triangleDict.TryGetValue(vertexA, out var trianglesWithVertexA))
        {
            for (int i = 0; i < trianglesWithVertexA.Count; ++i)
            {
                var triangle = trianglesWithVertexA[i];
                for (int j = 0; j < 3; ++j)
                {
                    var vertexB = triangle[j];
                    if (vertexA != vertexB && !m_checkedVertexes.Contains(vertexB) && IsOutlineEdge(vertexA, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    private bool IsOutlineEdge(int vertexA, int vertexB)
    {
        int sharedTriangles = 0;
        if (m_triangleDict.TryGetValue(vertexA, out var trianglesWithVertexA))
        {
            for (int i = 0; i < trianglesWithVertexA.Count; ++i)
            {
                if (trianglesWithVertexA[i].ContainsVertex(vertexB))
                {
                    sharedTriangles++;
                    if (sharedTriangles > 1)
                    {
                        break;
                    }
                }
            }
        }

        return sharedTriangles == 1;
    }

    private void CreateWallMesh()
    {
        CalculateMeshOutlines();
        
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();

        float wallHeight = 1f;
        foreach(var outline in m_outlines)
        {
            for (int i = 0; i < outline.Count - 1; ++i)
            {
                int startIndex = wallVertices.Count;
				wallVertices.Add(m_verticies[outline[i]]); // left
				wallVertices.Add(m_verticies[outline[i+1]]); // right
				wallVertices.Add(m_verticies[outline[i]] - Vector3.up * wallHeight); // bottom left
				wallVertices.Add(m_verticies[outline[i+1]] - Vector3.up * wallHeight); // bottom right

				wallTriangles.Add(startIndex + 0);
				wallTriangles.Add(startIndex + 2);
				wallTriangles.Add(startIndex + 3);

				wallTriangles.Add(startIndex + 3);
				wallTriangles.Add(startIndex + 1);
				wallTriangles.Add(startIndex + 0);
			}
		}
		wallMesh.vertices = wallVertices.ToArray ();
		wallMesh.triangles = wallTriangles.ToArray ();
        wallMesh.RecalculateBounds();
        wallMesh.RecalculateNormals();

		m_wallMeshFilter.mesh = wallMesh;
        m_wallCollider = m_wallMeshFilter.gameObject.AddComponent<MeshCollider>();
        m_wallCollider.sharedMesh = wallMesh;
        m_wallCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;

        PlacePOV(0.75f * wallHeight, 0.25f);
    }

    private void PlacePOV(float offsetY, float offset)
    {
        int povID = 0;
        foreach(var outline in m_outlines)
        {
            for (int i = 1; i < outline.Count - 1; ++i)
            {
                var vertA_pos = new Vector2(m_verticies[outline[i - 1]].x, m_verticies[outline[i - 1]].z);
                var vertB_pos = new Vector2(m_verticies[outline[i]].x, m_verticies[outline[i]].z);
                var vertC_pos = new Vector2(m_verticies[outline[i + 1]].x, m_verticies[outline[i + 1]].z);

                var a2b = (vertB_pos - vertA_pos);
                var b2c = (vertC_pos - vertB_pos);

                var a2bPerp = a2b.PerpendicularClockwise();
                var b2cPerp = b2c.PerpendicularClockwise();

                //
                Vector2 middlePoint2d = (vertB_pos + vertA_pos) / 2f;
                Vector3 rayPoint = new Vector3(middlePoint2d.x, 0, middlePoint2d.y);
                Vector3 rayDir = new Vector3(a2bPerp.x, 0, a2bPerp.y);
                //Debug.DrawRay(rayPoint, a2bPerp, Color.red, 300f);

                middlePoint2d = (vertC_pos + vertC_pos) / 2f;
                rayPoint = new Vector3(middlePoint2d.x, 0, middlePoint2d.y);;
                rayDir = new Vector3(b2cPerp.x, 0f, b2cPerp.y);
                //Debug.DrawRay(rayPoint, rayDir, Color.red, 300f);

                bool corner = Vector2.SignedAngle(b2cPerp, a2bPerp) > 0;
                if (corner)
                {
                    var offsetDir = offset * (a2b - b2c).normalized;
                    var offsetDir3D = new Vector3(offsetDir.x, 0, offsetDir.y);
                    var posPOV = new Vector3(vertB_pos.x, 0, vertB_pos.y) + offsetDir3D;

                    var pov = Instantiate(m_povPrefab, posPOV, Quaternion.identity, m_povContainer.transform);
                    pov.gameObject.name = povID.ToString();
                    povID++;
                    pov.radius = offset;
                }
            }
        }

        m_povContainer.transform.position = new Vector3(m_povContainer.transform.position.x, m_povContainer.transform.position.y - offsetY, m_povContainer.transform.position.z);
    }
}
