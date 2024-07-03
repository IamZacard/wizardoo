using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AudioManager audioManager = (AudioManager)target;

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Sound Entries", EditorStyles.boldLabel);

        foreach (var entry in audioManager.soundEntries)
        {
            EditorGUILayout.BeginHorizontal();

            entry.soundType = (AudioManager.SoundType)EditorGUILayout.EnumPopup(entry.soundType);
            entry.audioClip = (AudioClip)EditorGUILayout.ObjectField(entry.audioClip, typeof(AudioClip), false);

            EditorGUILayout.EndHorizontal();
        }
    }
}
