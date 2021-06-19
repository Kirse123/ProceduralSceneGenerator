using UnityEngine;
using UnityEditor;

namespace PathPlaning
{
    public class GraphAsset : ScriptableObject
    {
        private const string FOLDER = "Assets/Resources/NavigationGraphs";
        public static GraphAsset SaveGraphToAsset(NavigationGraph graph, string assetName)
        {
            var asset = CreateInstance(typeof(GraphAsset)) as GraphAsset;
            asset.Init(graph);

            AssetDatabase.CreateAsset(asset, $"{FOLDER}/{assetName}.asset");

            return asset;
        }
        
        private NavigationGraph m_graph;
        public NavigationGraph navigationGraph
        {
            get => m_graph;
        }

        private void Init(NavigationGraph graph)
        {
            this.m_graph = graph;
        }
    }
}
