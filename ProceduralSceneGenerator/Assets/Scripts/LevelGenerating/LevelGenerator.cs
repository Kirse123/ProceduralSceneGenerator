using UnityEngine;
using UnityEditor;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject m_wallPrefab;

    [SerializeField]
    private Vector2Int m_gridSize;

    [SerializeField]
    private Transform m_groundNode;
    
    [SerializeField]
    private bool m_generateBorders = true;

    private float m_wallDensity = 0.7f;
    public float wallDensity
    {
        get => m_wallDensity;
        internal set => m_wallDensity = value;
    }

    private bool m_isGenerated = false;
    public bool isGenerated => m_isGenerated;

    
    private void Awake()
    {
        m_wallDensity = Mathf.Clamp01(m_wallDensity);
        //GenerateLevel();
    }

    internal void GenerateLevel()
    {
        if (isGenerated)
        {
            ClearLevel();
        }

        var wallSize = m_wallPrefab.GetComponent<BoxCollider>().size;
        wallSize = Matrix4x4.Scale(m_wallPrefab.transform.localScale).MultiplyPoint(wallSize);

        int width = m_gridSize.x;
        int height = m_gridSize.y;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Should we place a wall?
                var shouldPlaceAsBorder = m_generateBorders && ((x == 0 || x == width - 1) || (y == 0 || y == height - 1));
				if (shouldPlaceAsBorder || Random.value > m_wallDensity)
				{
					// Spawn a wall
					Vector3 gridPos = new Vector3(x - width / 2f, 0f, y - height / 2f);
                    gridPos = Matrix4x4.Scale(wallSize).MultiplyPoint(gridPos);
                    gridPos += wallSize / 2f;
					Instantiate(m_wallPrefab, gridPos, Quaternion.identity, m_groundNode);
				} 
            }
        }

        m_isGenerated = true;
    }

    internal void ClearLevel()
    {
        for (int i = m_groundNode.childCount; i > 0; --i)
        {
            var wall = m_groundNode.GetChild(0);
            if (Application.isPlaying)
            {
                Destroy(wall.gameObject);
            }
            else
            {
                DestroyImmediate(wall.gameObject);
            }
        }
        m_isGenerated = false;
    }
}

[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var levelGenerator = target as LevelGenerator;
        
        // Draw slider for Wall density
        levelGenerator.wallDensity = 1f - EditorGUILayout.Slider("Wall density", 1f - levelGenerator.wallDensity, 0f, 1f);
        
        // Draw buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate level") && levelGenerator)
        {
            levelGenerator.GenerateLevel();
        }
        if (GUILayout.Button("Clear level"))
        {
            levelGenerator.ClearLevel();
        }
        GUILayout.EndHorizontal();
    }
}
