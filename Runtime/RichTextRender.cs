namespace UnityEngine.UI
{
    [ExecuteInEditMode]
    public class RichTextRender : MonoBehaviour
    {
        [System.NonSerialized]
        private MeshRenderer m_meshRender;
        [System.NonSerialized]
        private MeshFilter m_meshFilter;
        [System.NonSerialized]
        private Mesh m_mesh;

        private bool m_dirty;

        // Cached CombineInstance array to avoid repeated allocations
        private CombineInstance[] m_combineCache = new CombineInstance[0];

        internal static readonly HideFlags MeshHideflags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInInspector;

        struct MeshOrder
        {
            public Mesh mesh;
            public float z;
            public Matrix4x4 matrix;
        }

        // Static mesh sorter to avoid Lambda GC allocation
        private static readonly System.Comparison<MeshOrder> s_meshSorter = (lhs, rhs) => rhs.z.CompareTo(lhs.z);

        public void MarkDirty()
        {
            m_dirty = true;
#if UNITY_EDITOR
            OnCombineMesh();
#endif
        }

        private void OnCombineMesh()
        {
            RichText[] texts = GetComponentsInChildren<RichText>(false);
            RichImage[] images = GetComponentsInChildren<RichImage>(false);
            var meshCount = texts.Length + images.Length;
            if (meshCount == 0)
            {
                if (m_mesh)
                {
                    m_mesh.Clear();
                }
                if (m_meshRender)
                {
                    m_meshRender.enabled = false;
                }
                return;
            }

            if (m_meshRender == null)
            {
                m_meshRender = gameObject.GetOrAddComponent<MeshRenderer>();
                m_meshRender.hideFlags = MeshHideflags;

                m_meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
                m_meshFilter.hideFlags = MeshHideflags;

                m_mesh = new Mesh();
                m_mesh.MarkDynamic();
                m_mesh.hideFlags = MeshHideflags;
            }

            Material material = null;

            var meshes = ListPool<MeshOrder>.Get();

            var worldToLocalMatrix = this.transform.worldToLocalMatrix;
            for (int i = 0; i < images.Length; ++i)
            {
                var image = images[i];

                if (!image.IsActive())
                {
                    continue;
                }

                var mesh = image.Mesh();
                if (mesh == null)
                {
                    continue;
                }

                var meshOrder = new MeshOrder();
                meshOrder.mesh = mesh;
                meshOrder.matrix = worldToLocalMatrix * image.transform.localToWorldMatrix;
                meshOrder.z = image.transform.localPosition.z;

                meshes.Add(meshOrder);

                if (material == null)
                {
                    material = image.material;
                }
            }
            for (int j = 0; j < texts.Length; ++j)
            {
                var text = texts[j];

                if (!text.IsActive())
                {
                    continue;
                }

                var mesh = text.Mesh();
                if (mesh == null)
                {
                    continue;
                }

                var meshOrder = new MeshOrder();
                meshOrder.mesh = mesh;
                meshOrder.matrix = worldToLocalMatrix * text.transform.localToWorldMatrix;
                meshOrder.z = text.transform.localPosition.z;

                meshes.Add(meshOrder);

                if (material == null)
                {
                    material = text.material;
                }
            }

            if (meshes.Count == 0)
            {
                if (m_meshRender)
                {
                    m_meshRender.enabled = false;
                }
                return;
            }
            m_meshRender.enabled = true;

            // Use static sorter to avoid Lambda GC
            meshes.Sort(s_meshSorter);

            // Reuse or expand cached CombineInstance array to avoid repeated allocations
            if (m_combineCache.Length < meshes.Count)
            {
                m_combineCache = new CombineInstance[meshes.Count * 2];
            }

            meshCount = meshes.Count;
            for (int i = 0; i < meshCount; ++i)
            {
                m_combineCache[i].mesh = meshes[i].mesh;
                m_combineCache[i].transform = meshes[i].matrix;
            }

            ListPool<MeshOrder>.Release(meshes);

            // Only pass the exact number of valid CombineInstances to avoid null mesh errors
            // Create a temporary array slice without allocating if possible
            CombineInstance[] combineArray;
            if (m_combineCache.Length == meshCount)
            {
                combineArray = m_combineCache;
            }
            else
            {
                combineArray = new CombineInstance[meshCount];
                System.Array.Copy(m_combineCache, combineArray, meshCount);
            }

            m_mesh.CombineMeshes(combineArray, true, true, false);
            m_meshFilter.sharedMesh = m_mesh;
            m_meshRender.sharedMaterial = material;
        }

        protected void OnEnable()
        {
            m_dirty = true;
        }

        private void LateUpdate()
        {
            if (m_dirty)
            {
                OnCombineMesh();

                m_dirty = false;
            }
        }

    }

}