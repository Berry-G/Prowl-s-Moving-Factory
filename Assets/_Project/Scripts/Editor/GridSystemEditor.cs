using UnityEditor;
using UnityEngine;
using PMF.Grid;

namespace PMF.EditorTools
{
    /// <summary>GridSystem 인스펙터에 "타일맵에서 다시 읽기" 버튼 (P-04 DoD).</summary>
    [CustomEditor(typeof(GridSystem))]
    public sealed class GridSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var gridSystem = (GridSystem)target;
            if (GUILayout.Button("타일맵에서 다시 읽기"))
            {
                Undo.RecordObject(gridSystem, "Rebuild cells from tilemaps");
                gridSystem.BuildFromTilemaps();
                EditorUtility.SetDirty(gridSystem);
            }
        }
    }
}
