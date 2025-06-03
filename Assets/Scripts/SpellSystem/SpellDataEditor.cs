#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpellData))]
public class SpellDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SpellData spellData = (SpellData)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Add Spell Effect", EditorStyles.boldLabel);

        if (GUILayout.Button("Add TeleportEffect"))
        {
            spellData.effects.Add(new TeleportEffect());
            EditorUtility.SetDirty(spellData);
        }

        if (GUILayout.Button("Add MakeInvulnerableEffect"))
        {
            spellData.effects.Add(new MakeInvulnerableEffect());
            EditorUtility.SetDirty(spellData);
        }

        // Add more buttons for other SpellEffectBase types here
    }
}
#endif
