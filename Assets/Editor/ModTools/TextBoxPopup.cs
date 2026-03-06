using System;
using UnityEditor;
using UnityEngine;

namespace Editor.ModTools
{
  internal class TextBoxPopup : EditorWindow
  {
    private string _inputText = "";
    private Action<string> _onSubmit;

    public static void ShowWindow(Action<string> onSubmit)
    {
      var window = CreateInstance<TextBoxPopup>();
      window.titleContent = new GUIContent("New Mod");
      window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
      window._onSubmit = onSubmit;
      window.ShowUtility();
    }
    
    private void OnGUI()
    {
      GUILayout.Label("Please enter mod name:", EditorStyles.boldLabel);
      _inputText = EditorGUILayout.TextField("Input", _inputText);

      GUILayout.Space(10);
      if (!GUILayout.Button("Create")) return;
      _onSubmit?.Invoke(_inputText);
      Close();
    }
  }
}