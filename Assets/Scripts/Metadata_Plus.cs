using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metadata_Plus : MonoBehaviour
{
    [Header("Dictionary Editor")]
    public string key;
    public string value;
    public bool createParameterFromInspector = false;
    [Space]
    public StringDict parameters = new StringDict();

    private void OnValidate()
    {
        if (createParameterFromInspector)
        {
            createParameterFromInspector = false;

            if (parameters.ContainsKey(key)) parameters.Remove(key);
            parameters.Add(key, value);
        }
    }
}

[Serializable]
public class StringDict : Dictionary<string, string>, ISerializationCallbackReceiver
{
    [SerializeField]
    List<string> m_Keys = new List<string>();

    [SerializeField]
    List<string> m_Values = new List<string>();

    public void OnBeforeSerialize()
    {
        m_Keys.Clear();
        m_Values.Clear();

        foreach (KeyValuePair<string, string> kvp in this)
        {
            m_Keys.Add(kvp.Key);
            m_Values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        for (int i = 0; i < m_Keys.Count; i++)
        {
            if (ContainsKey(m_Keys[i])) Remove(m_Keys[i]);

            Add(m_Keys[i], m_Values[i]);
        }

        m_Keys.Clear();
        m_Values.Clear();
    }
}