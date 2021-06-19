using System.Collections.Generic;
using UnityEngine;

public class PointOfVisibilityPlacer : MonoBehaviour
{
    private float GIZMOS_SPHERE_TRANSPERACY = 0.5f;
    
    [SerializeField]
    private bool m_drawGizmos = true;
    
    [SerializeField]
    private float m_radius = 0.1f;
    public float radius
    {
        get => m_radius;
        set => m_radius = value;
    }

    public Vector3 position
    {
        get
        {
            return transform.position;
        }
    }

    private void OnDrawGizmos()
    {
        if (!m_drawGizmos)
        {
            return;
        }

        var colorPrev = Gizmos.color;

        var colorNew = Color.red;
        colorNew.a = GIZMOS_SPHERE_TRANSPERACY;
        Gizmos.color = colorNew;

        Gizmos.DrawSphere(position, m_radius);
        Gizmos.color = colorPrev;
    }
}
