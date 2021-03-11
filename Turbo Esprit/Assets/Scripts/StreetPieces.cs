using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace TurboEsprit
{
    public class StreetPieces : MonoBehaviour
    {
        public GameObject roadPrefab;
        public GameObject sidewalkPrefab;
        public GameObject sidewalkCornerPrefab;
        public GameObject solidLinePrefab;
        public GameObject brokenLinePrefab;

        public static void ChangeBrokenLineTiling(GameObject brokenLine)
        {
            ProBuilderMesh proBuilderMesh = brokenLine.GetComponent<ProBuilderMesh>();

            // Find where in the world the line is positioned, so that we can tile it in global space.
            bool roadIsNortSouth = brokenLine.transform.rotation.y == 0;
            float startPosition = roadIsNortSouth ? brokenLine.transform.position.z : brokenLine.transform.position.x;
            float length = brokenLine.transform.localScale.z;

            // Update UVs to match broken line spacing.
            Vertex[] vertices = proBuilderMesh.GetVertices();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vertex vertex = vertices[i];
                Vector2 uv = vertex.uv0;

                float position = startPosition + vertex.uv0.y * length;
                uv.y = position / City.brokenLineSpacing;

                vertex.uv0 = uv;
            }

            proBuilderMesh.SetVertices(vertices, true);
        }

        public GameObject Instantiate(GameObject prefab, GameObject parent)
        {
            return Instantiate<GameObject>(prefab, parent.transform);
        }


    }
}
