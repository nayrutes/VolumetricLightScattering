using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class TextChanger : MonoBehaviour
{
    public TMP_Text textField;
    public List<string> texts;


    public void SetMyText(int i)
    {
        if (i < texts.Count && i>= 0)
        {
            textField.text = texts[i];
        }
    }
    public void SetMyText(string text)
    {
        textField.text = text;
    }
}
