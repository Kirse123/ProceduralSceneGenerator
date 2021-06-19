using UnityEngine;

public class WaypointPlacer : MonoBehaviour
{
    [SerializeField]
    private bool m_drawGizmos = true;
    
    public Vector3 position
    {
        get => transform.position;
    }

    private void OnDrawGizmos()
    {
        if (m_drawGizmos)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(position, 0.25f);
        }
    }
}
