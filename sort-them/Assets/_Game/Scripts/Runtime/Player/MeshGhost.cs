using UnityEngine;

namespace SortThem
{
    public class MeshGhost : MonoBehaviour
    {
        public MeshFilter Filter;
        public MeshRenderer Renderer;

        public void Show(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (Filter.sharedMesh != mesh) Filter.sharedMesh = mesh;
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
}
