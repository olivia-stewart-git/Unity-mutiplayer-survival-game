using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildRepresentor : MonoBehaviour
{
    public CollisionBounds[] bounds;
    public MeshRenderer m_Renderer;
    public SnapPoint[] snapPoints;

    private MaterialPropertyBlock _PropBlock;

    private void Awake()
    {
        _PropBlock = new MaterialPropertyBlock();
    }

    public void OnDrawGizmosSelected()
    {
        //draw gizmos
        Gizmos.color = Color.yellow;
        if (snapPoints != null)
        {
            if (snapPoints.Length > 0)
            {
                foreach (SnapPoint sPoint in snapPoints)
                {
                    Gizmos.DrawSphere(sPoint.point.position, 0.1f);
                    Gizmos.DrawLine(sPoint.point.position, sPoint.point.position + (sPoint.point.forward * 0.2f));
                }
            }
        }

        //show the collision bounds
        Gizmos.color = new Color(255, 0, 0, 0.5f);
        if (bounds != null)
        {
            foreach (CollisionBounds col_bound in bounds)
            {
                Vector3 centre = ((col_bound.bottomBounds.position + ((col_bound.topBounds.position - col_bound.bottomBounds.position) * 0.5f) - transform.position));
                
                Vector3 size = (col_bound.topBounds.localPosition - col_bound.bottomBounds.localPosition);

                Matrix4x4 m4 = transform.localToWorldMatrix;
                Gizmos.matrix = m4;
                Gizmos.DrawCube(centre, size);
            }
        }
    }


    public void SetRepresentorColor(Color color)
    {
        m_Renderer.GetPropertyBlock(_PropBlock);


        _PropBlock.SetColor("_BaseColor", color);

        m_Renderer.SetPropertyBlock(_PropBlock);
    }

}
