using UnityEngine;

namespace Alfredo
{
    /// <summary>Ordena por altura: lo que está más abajo en pantalla se dibuja delante.</summary>
    public class YSort : MonoBehaviour
    {
        [SerializeField] bool isStatic;
        [SerializeField] int offset;
        SpriteRenderer[] renderers;

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            Apply();
        }

        void LateUpdate()
        {
            if (!isStatic) Apply();
        }

        void Apply()
        {
            int order = Mathf.RoundToInt(-transform.position.y * 10f) + offset;
            foreach (var r in renderers) r.sortingOrder = order;
        }
    }
}
