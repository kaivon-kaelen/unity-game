using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GI
{
    [ExecuteAlways]
    public class SDF : MonoBehaviour
    {
        public struct MeshEntry
        {
            public Matrix4x4 Transform;
            public Material Material;
            public Mesh Mesh;
        }

        public IEnumerable<MeshEntry> Meshes { get { return _meshes; } }

        public Bounds BaseBounds { get { return _base_bounds; } }

        [HideInInspector]
        public string UniqueID;

        [SerializeReference]
        public SDFEntry Entry;

        [Range(-0.3f, 0.3f)]
        public float Bias = 0.01f;

        [Range(8, 64)]
        public int Resolution = 32;

        public bool ForceDoubleSided;

        public bool PreviewWhenSelected = false;

        [NonSerialized]
        internal int RenderId = -1;

        private List<MeshEntry> _meshes = new List<MeshEntry>();
        private Bounds _base_bounds = new Bounds();
        private Vector3[] _bounds = new Vector3[8];
        private bool _has_base_bounds = false;

        private SDFAtlas _atlas;

        private void OnEnable()
        {
            _atlas = FindObjectOfType<SDFAtlas>();

            if (_atlas != null)
                _atlas.Register(this);
        }

        private void OnDisable()
        {
            if (_atlas != null)
            {
                _atlas.Unregister(this);
                _atlas = null;
            }
        }

        public void UpdateEntries()
        {
            _meshes.Clear();

            _has_base_bounds = false;
            _base_bounds = new Bounds(Vector3.zero, Vector3.zero);

            var lod_group = GetComponent<LODGroup>();

            if (lod_group != null && lod_group.lodCount > 0)
            {
                foreach (var renderer in lod_group.GetLODs()[0].renderers)
                    addEntry(renderer);
            }
            else
            {
                foreach (var renderer in GetComponentsInChildren<Renderer>())
                    addEntry(renderer);
            }
        }

        private void addEntry(Renderer renderer)
        {
            var mesh_filter = renderer.GetComponent<MeshFilter>();

            if (mesh_filter == null)
            {
                Debug.LogError("SDF requires meshes");
                return;
            }

            var mesh = mesh_filter.sharedMesh;

            if (mesh == null)
            {
                Debug.LogError("SDF requires meshes");
                return;
            }

            var material = renderer.sharedMaterial;

            if (material == null)
            {
                Debug.LogError("SDF requires materials");
                return;
            }

            if (mesh.GetTopology(0) != MeshTopology.Triangles)
            {
                Debug.LogError("SDF requires triangles");
                return;
            }

            if (mesh.indexFormat != IndexFormat.UInt16)
            {
                Debug.LogError("SDF requires 16 bit indices");
                return;
            }

            int positions = mesh.GetVertexAttributeStream(VertexAttribute.Position);

            if (positions < 0)
            {
                Debug.LogError("SDF requires positions");
                return;
            }

            var entry = new MeshEntry();
            entry.Mesh = mesh;
            entry.Material = material;

            var transform = Matrix4x4.identity;

            var cursor = renderer.gameObject;

            while (cursor != gameObject)
            {
                transform = Matrix4x4.Translate(cursor.transform.localPosition) *
                            Matrix4x4.Rotate(cursor.transform.localRotation) *
                            Matrix4x4.Scale(cursor.transform.localScale) *
                            transform;

                cursor = cursor.gameObject.transform.parent.gameObject;
            }

            entry.Transform = transform;

            _meshes.Add(entry);

            var min = renderer.localBounds.min;
            var max = renderer.localBounds.max;
            _bounds[0] = new Vector3(min.x, min.y, min.z);
            _bounds[1] = new Vector3(max.x, min.y, min.z);
            _bounds[2] = new Vector3(min.x, max.y, min.z);
            _bounds[3] = new Vector3(max.x, max.y, min.z);
            _bounds[4] = new Vector3(min.x, min.y, max.z);
            _bounds[5] = new Vector3(max.x, min.y, max.z);
            _bounds[6] = new Vector3(min.x, max.y, max.z);
            _bounds[7] = new Vector3(max.x, max.y, max.z);

            var bounds = GeometryUtility.CalculateBounds(_bounds, entry.Transform);

            if (_has_base_bounds)
                _base_bounds.Encapsulate(bounds);
            else
            {
                _has_base_bounds = true;
                _base_bounds = bounds;
            }
        }
    }
}