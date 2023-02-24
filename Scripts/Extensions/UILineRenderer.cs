using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : MaskableGraphic
{
    public float LineThikness = 2;
    public bool UseMargins;
    public Vector2 Margin;
    public Vector2[] Points;

    protected override void OnFillVBO(List<UIVertex> vbo)
    {
        if (Points == null || Points.Length < 2)
            Points = new[] { new Vector2(0, 0), new Vector2(1, 1) };

        var sizeX = rectTransform.rect.width;
        var sizeY = rectTransform.rect.height;
        var offsetX = -rectTransform.pivot.x * rectTransform.rect.width;
        var offsetY = -rectTransform.pivot.y * rectTransform.rect.height;

        if (UseMargins)
        {
            sizeX -= Margin.x;
            sizeY -= Margin.y;
            offsetX += Margin.x / 2f;
            offsetY += Margin.y / 2f;
        }

        vbo.Clear();

        Vector2 prevV1 = Vector2.zero;
        Vector2 prevV2 = Vector2.zero;

        for (int i = 1; i < Points.Length; i++)
        {
            var prev = Points[i - 1];
            var cur = Points[i];
            prev = new Vector2(prev.x * sizeX + offsetX, prev.y * sizeY + offsetY);
            cur = new Vector2(cur.x * sizeX + offsetX, cur.y * sizeY + offsetY);

            float angle = Mathf.Atan2(cur.y - prev.y, cur.x - prev.x) * 180f / Mathf.PI;

            var v1 = prev + new Vector2(0, -LineThikness / 2);
            var v2 = prev + new Vector2(0, +LineThikness / 2);
            var v3 = cur + new Vector2(0, +LineThikness / 2);
            var v4 = cur + new Vector2(0, -LineThikness / 2);

            v1 = RotatePointAroundPivot(v1, prev, new Vector3(0, 0, angle));
            v2 = RotatePointAroundPivot(v2, prev, new Vector3(0, 0, angle));
            v3 = RotatePointAroundPivot(v3, cur, new Vector3(0, 0, angle));
            v4 = RotatePointAroundPivot(v4, cur, new Vector3(0, 0, angle));

            if (i > 1)
                SetVbo(vbo, new[] { prevV1, prevV2, v1, v2 });

            SetVbo(vbo, new[] { v1, v2, v3, v4 });

            prevV1 = v3;
            prevV2 = v4;
        }
    }

    protected void SetVbo(List<UIVertex> vbo, Vector2[] vertices)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            var vert = UIVertex.simpleVert;
            vert.color = color;
            vert.position = vertices[i];
            vbo.Add(vert);
        }
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
    }
}
