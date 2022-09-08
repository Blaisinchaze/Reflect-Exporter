using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(Metadata_Plus))]
public class Metadata_Plus_Editor : Editor
{
    static GUIStyle s_BoldFoldout;
    bool showParameters = false;
    bool addParameter = false;
    bool deleteParameter = false;

    string newParameter = "";
    string newValue = "";
    int selectedParameter = 0;

    [Serializable]
    class ParameterGroup
    {
        public string name;
        public string value;
    }

    List<ParameterGroup> parameterGroups;
    public override void OnInspectorGUI()
    {
        var model = (Metadata_Plus)target;

        if (parameterGroups == null)
        {
            LoadInData(model);
        }

        if (s_BoldFoldout == null)
        {
            s_BoldFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
        }

        showParameters = EditorGUILayout.Foldout(showParameters, "Loaded Parameters", true, s_BoldFoldout);

        if (showParameters)
        {
            foreach (var _parameterGroups in parameterGroups)
            {
                _parameterGroups.value = EditorGUILayout.TextField(_parameterGroups.name, _parameterGroups.value);
            }
        }

        GUILayout.Space(15);

        addParameter = EditorGUILayout.Foldout(addParameter, "Add Parameter", true, s_BoldFoldout);

        if(addParameter)
        {
            newParameter = EditorGUILayout.TextField("New Parameter : ", newParameter);
            newValue = EditorGUILayout.TextField("New Parameter's Value : ", newValue);
            EditorGUI.BeginDisabledGroup(newParameter == "");
            if (GUILayout.Button("Save value"))
            {
                if (model.parameters.Keys.Contains(newParameter))
                {
                    Debug.Log("That Parameter already exists");
                    return;
                }
                model.parameters.Add(newParameter, newValue);
                newParameter = "";
                newValue = "";
                LoadInData(model);
            }
            EditorGUI.EndDisabledGroup();
        }

        GUILayout.Space(15);

        deleteParameter = EditorGUILayout.Foldout(deleteParameter, "Delete Parameter", true, s_BoldFoldout);

        if (deleteParameter)
        {
            string[] options = model.parameters.Keys.ToArray();
            selectedParameter = EditorGUILayout.Popup("Parameter to remove : ", selectedParameter, options);

            if (GUILayout.Button("Delete Parameter"))
            {                
                Debug.Log("Removed " + options[selectedParameter]);
                model.parameters.Remove(options[selectedParameter]);
                selectedParameter = 0;

                LoadInData(model);
            }
        }
    }

    void LoadInData(Metadata_Plus _model)
    {
        parameterGroups = new List<ParameterGroup>();
        foreach (var _parameter in _model.parameters)
        {
            var group = new ParameterGroup
            {
                name = _parameter.Key,
                value = _parameter.Value
            };

            parameterGroups.Add(group);
        }
    }

}
