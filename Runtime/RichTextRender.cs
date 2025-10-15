using System.Collections.Generic;

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

        // Support for multiple sub-meshes when vertex count exceeds limit
        [System.NonSerialized]
        private List<MeshRenderer> m_subMeshRenderers;
        [System.NonSerialized]
        private List<MeshFilter> m_subMeshFilters;
        [System.NonSerialized]
        private List<Mesh> m_subMeshes;

        private bool m_dirty;

        // Cached CombineInstance array to avoid repeated allocations
        private CombineInstance[] m_combineCache = new CombineInstance[0];

        // Unity mesh vertex limit (16-bit index buffer)
        private const int MAX_VERTICES_PER_MESH = 65000; // Safety margin below 65535

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
                ClearAllMeshes();
                return;
            }

            Material material = null;
            var meshes = ListPool<MeshOrder>.Get();

            var worldToLocalMatrix = this.transform.worldToLocalMatrix;

            // Collect all image meshes
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

            // Collect all text meshes
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
                ClearAllMeshes();
                ListPool<MeshOrder>.Release(meshes);
                return;
            }

            // Sort by Z order to maintain rendering order
            meshes.Sort(s_meshSorter);

            // Calculate total vertex count
            int totalVertices = 0;
            for (int i = 0; i < meshes.Count; ++i)
            {
                totalVertices += meshes[i].mesh.vertexCount;
            }

            // Check if we need to split into multiple meshes
            if (totalVertices <= MAX_VERTICES_PER_MESH)
            {
                // Single mesh path - optimal performance
                CombineSingleMesh(meshes, material);
            }
            else
            {
                // Multiple meshes path - handle vertex overflow
                CombineMultipleMeshes(meshes, material, totalVertices);
            }

            ListPool<MeshOrder>.Release(meshes);
        }

        private void ClearAllMeshes()
        {
            if (m_mesh)
            {
                m_mesh.Clear();
            }
            if (m_meshRender)
            {
                m_meshRender.enabled = false;
            }

            // Clear sub-meshes
            if (m_subMeshes != null)
            {
                for (int i = 0; i < m_subMeshes.Count; ++i)
                {
                    if (m_subMeshes[i])
                    {
                        m_subMeshes[i].Clear();
                    }
                }
            }
            if (m_subMeshRenderers != null)
            {
                for (int i = 0; i < m_subMeshRenderers.Count; ++i)
                {
                    if (m_subMeshRenderers[i])
                    {
                        m_subMeshRenderers[i].enabled = false;
                    }
                }
            }
        }

        private void CombineSingleMesh(List<MeshOrder> meshes, Material material)
        {
            // Ensure primary mesh renderer exists
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

            m_meshRender.enabled = true;

            // Disable sub-mesh renderers if they exist
            if (m_subMeshRenderers != null)
            {
                for (int i = 0; i < m_subMeshRenderers.Count; ++i)
                {
                    if (m_subMeshRenderers[i])
                    {
                        m_subMeshRenderers[i].enabled = false;
                    }
                }
            }

            // Reuse or expand cached CombineInstance array
            if (m_combineCache.Length < meshes.Count)
            {
                m_combineCache = new CombineInstance[meshes.Count * 2];
            }

            int meshCount = meshes.Count;
            for (int i = 0; i < meshCount; ++i)
            {
                m_combineCache[i].mesh = meshes[i].mesh;
                m_combineCache[i].transform = meshes[i].matrix;
            }

            // Create array slice for CombineMeshes
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

        private void CombineMultipleMeshes(List<MeshOrder> meshes, Material material, int totalVertices)
        {
            // Log warning about vertex overflow
            Debug.LogWarning($"[RichTextRender] Total vertices ({totalVertices}) exceeds limit ({MAX_VERTICES_PER_MESH}). Splitting into multiple meshes. This will increase drawcalls.", this);

            // Initialize sub-mesh containers
            if (m_subMeshRenderers == null)
            {
                m_subMeshRenderers = new List<MeshRenderer>();
                m_subMeshFilters = new List<MeshFilter>();
                m_subMeshes = new List<Mesh>();
            }

            // Calculate required sub-mesh count
            int estimatedSubMeshCount = (totalVertices / MAX_VERTICES_PER_MESH) + 1;

            // Prepare sub-mesh batches
            var subMeshBatches = ListPool<List<MeshOrder>>.Get();
            List<MeshOrder> currentBatch = null;
            int currentBatchVertices = 0;

            for (int i = 0; i < meshes.Count; ++i)
            {
                var meshOrder = meshes[i];
                int vertexCount = meshOrder.mesh.vertexCount;

                // Check if we need a new batch
                if (currentBatch == null || currentBatchVertices + vertexCount > MAX_VERTICES_PER_MESH)
                {
                    currentBatch = ListPool<MeshOrder>.Get();
                    subMeshBatches.Add(currentBatch);
                    currentBatchVertices = 0;
                }

                currentBatch.Add(meshOrder);
                currentBatchVertices += vertexCount;
            }

            // Ensure we have enough sub-mesh objects
            while (m_subMeshes.Count < subMeshBatches.Count)
            {
                var subMesh = new Mesh();
                subMesh.MarkDynamic();
                subMesh.hideFlags = MeshHideflags;
                m_subMeshes.Add(subMesh);

                var subRenderer = gameObject.AddComponent<MeshRenderer>();
                subRenderer.hideFlags = MeshHideflags;
                m_subMeshRenderers.Add(subRenderer);

                var subFilter = gameObject.AddComponent<MeshFilter>();
                subFilter.hideFlags = MeshHideflags;
                m_subMeshFilters.Add(subFilter);
            }

            // Disable primary mesh renderer
            if (m_meshRender)
            {
                m_meshRender.enabled = false;
            }

            // Combine each batch into a sub-mesh
            for (int batchIndex = 0; batchIndex < subMeshBatches.Count; ++batchIndex)
            {
                var batch = subMeshBatches[batchIndex];
                var subMesh = m_subMeshes[batchIndex];
                var subRenderer = m_subMeshRenderers[batchIndex];
                var subFilter = m_subMeshFilters[batchIndex];

                // Prepare combine instances
                CombineInstance[] combineArray = new CombineInstance[batch.Count];
                for (int i = 0; i < batch.Count; ++i)
                {
                    combineArray[i].mesh = batch[i].mesh;
                    combineArray[i].transform = batch[i].matrix;
                }

                // Combine and assign
                subMesh.CombineMeshes(combineArray, true, true, false);
                subFilter.sharedMesh = subMesh;
                subRenderer.sharedMaterial = material;
                subRenderer.enabled = true;

                ListPool<MeshOrder>.Release(batch);
            }

            // Disable unused sub-mesh renderers
            for (int i = subMeshBatches.Count; i < m_subMeshRenderers.Count; ++i)
            {
                if (m_subMeshRenderers[i])
                {
                    m_subMeshRenderers[i].enabled = false;
                }
            }

            ListPool<List<MeshOrder>>.Release(subMeshBatches);
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