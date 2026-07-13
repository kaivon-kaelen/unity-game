using UnityEditor;
using UnityEngine;

namespace GI
{
    [CustomEditor(typeof(SDFAtlas))]
    [CanEditMultipleObjects]
    public class SDFAtlasEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Find Terrain"))
            {
                foreach (var obj in Selection.gameObjects)
                {
                    var atlas = obj.GetComponent<SDFAtlas>();

                    if (atlas == null)
                        continue;

                    Undo.RecordObject(atlas, "Force Find Terrain");

                    if (atlas.Terrain == null)
                        atlas.Terrain = new SDFTerrain();

                    atlas.Terrain.Terrain = atlas.Terrain.Find(Vector3.zero);

                    if (atlas.Terrain.Terrain != null)
                    {
                        var terrain = atlas.Terrain.Terrain.GetComponent<Terrain>();

                        atlas.Terrain.Copy(terrain);
                    }
                }
            }
        }
    }
}