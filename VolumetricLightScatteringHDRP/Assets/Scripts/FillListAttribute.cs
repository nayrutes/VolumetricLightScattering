using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[AttributeUsage(AttributeTargets.Field)]
public class FillListAttribute : PropertyAttribute
{
}

[CustomPropertyDrawer(typeof(FillListAttribute))]
public class FillListDrawer : PropertyDrawer
{
    private const string TYPE_ERROR = "FillList can only be used on List fields.";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        return base.CreatePropertyGUI(property);
    }

//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        base.OnGUI(position, property, label);
////        if (!property.type.StartsWith("PPtr<"))
////        {
////            GUIStyle errorStyle = "CN EntryErrorIconSmall";
////            Rect r = new Rect(position);
////            r.width = errorStyle.fixedWidth;
////            position.xMin = r.xMax;
////            GUI.Label(r, "", errorStyle);
////            GUI.Label(position, TYPE_ERROR);
////            return;
////        }
//
////        if (property.objectReferenceValue == null)
////        {
////            property.objectReferenceValue = Instantia;
////        }
//
////        property.serializedObject.ApplyModifiedProperties();
////        if (!property.isArray)
////            return;
////        int l = 0;
////
////        property.Next(true);
////        property.Next(true);
////
////        l = property.intValue;
////
////        property.Next(true);
////        
////        for (int i = 0; i < l-1; i++)
////        {
////            property.InsertArrayElementAtIndex(i);
////        }
//    }
}