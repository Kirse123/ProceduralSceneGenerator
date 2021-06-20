using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using SurroundingGenerating;

public class MapGenerator : MonoBehaviour
{
    private delegate float NoiseFunction(int x, int y);

    [SerializeField]
    private NavGraph m_navGraph;
    [SerializeField]
    private bool m_drawGizmos = false;
    
    [SerializeField][Range(0, 1)]
    private float m_dencity;

    [SerializeField]
    private int m_smoothParam;
    [SerializeField]
    private int m_smoothCycles;

    [SerializeField]
    private Vector2Int m_mapSize;

    private Mesh m_mesh;
    private Mesh m_meshWall;

    private int[,] m_map;
    private MapDataGenerator2D.MapCell[,] m_cellMap;
    
    public void Start()
    {
        StartCoroutine(GenerateMapCoroutine());
    }

    internal void Generate()
    {
        MapDataGenerator2D.InitMap(ref m_map, m_mapSize, m_dencity);
        for (int i = 0; i < m_smoothCycles; ++i)
        {
            MapDataGenerator2D.SmoothMap(ref m_map, m_smoothParam);
        }

        int borderSize = 1;
        int[,] borderedMap = new int[m_mapSize.x + borderSize, m_mapSize.y + borderSize];
        for (int x = 0; x < borderedMap.GetLength(0); x ++) 
        {
            for (int y = 0; y < borderedMap.GetLength(1); y ++) 
            {
                if (x > borderSize && x < m_mapSize.x + borderSize && y > borderSize && y < m_mapSize.y + borderSize)
                {
                    borderedMap[x, y] = m_map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }

        
        var meshGenerator = GetComponent<MeshGenerator>();
        meshGenerator.GenerateMesh(borderedMap, 5f);
        m_mesh = meshGenerator.meshFilter.mesh;
        m_meshWall = meshGenerator.wallMeshFilter.mesh;
    }

    private enum CoroutineState
    {
        None,
        Init,
        Smooth,
        Mesh,
        posPOV

    }
    private CoroutineState m_state = CoroutineState.None;
    private IEnumerator GenerateMapCoroutine()
    {
        MapDataGenerator2D.InitMap(ref m_map, m_mapSize, m_dencity);
        m_state = CoroutineState.Init;
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < m_smoothCycles; ++i)
        {
            MapDataGenerator2D.SmoothMap(ref m_map, m_smoothParam);
            m_state = CoroutineState.Smooth;
            yield return new WaitForSeconds(1f);
        }

        int borderSize = 1;
        int[,] borderedMap = new int[m_mapSize.x + borderSize, m_mapSize.y + borderSize];
        for (int x = 0; x < borderedMap.GetLength(0); x ++) 
        {
            for (int y = 0; y < borderedMap.GetLength(1); y ++) 
            {
                if (x >= borderSize && x < m_mapSize.x + borderSize && y >= borderSize && y < m_mapSize.y + borderSize)
                {
                    borderedMap[x, y] = m_map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }
        m_map = borderedMap;
        yield return new WaitForSeconds(1f);

        m_state = CoroutineState.Mesh;
        var meshGenerator = GetComponent<MeshGenerator>();
        meshGenerator.GenerateMesh(borderedMap, 1f);
        yield return new WaitForSeconds(1f);

        m_state = CoroutineState.posPOV;
        m_navGraph.BakeNavGraph();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MapDataGenerator2D.SmoothMap(ref m_map, m_smoothParam);
        }
    }

    
    private const string PATH = "Assets/Resources/Meshes/";
    internal void SaveMeshToAssets()
    {
        var name = $"{PATH}{System.Guid.NewGuid().ToString().Remove(7)}";

        AssetDatabase.CreateAsset(m_mesh, $"{name}.asset");
        AssetDatabase.CreateAsset(m_meshWall, $"{name}_walls.asset");
        AssetDatabase.SaveAssets();
    }

	void OnDrawGizmos() 
    {
		if (m_map != null && m_state == CoroutineState.Init || m_state == CoroutineState.Smooth) 
        {
            var sz = new Vector2Int(m_map.GetUpperBound(0) + 1, m_map.GetUpperBound(1) + 1);
            for (int x = 0; x < sz.x; x ++) 
            {
				for (int y = 0; y < sz.y; y ++) 
                {
					Gizmos.color = (m_map[x,y] == 1)?Color.black:Color.white;

					Vector3 pos = new Vector3(-sz.x/2 + x + .5f,0, -sz.y/2 + y+.5f);
					Gizmos.DrawCube(pos,Vector3.one);
				}
			}
		}
	}
}

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var mapGenerator = target as MapGenerator;
        
        // Draw buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate") && mapGenerator)
        {
            mapGenerator.Generate();
        }
        if (GUILayout.Button("Clear level") && mapGenerator)
        {
            mapGenerator.SaveMeshToAssets();
        }
        GUILayout.EndHorizontal();
    }
}
