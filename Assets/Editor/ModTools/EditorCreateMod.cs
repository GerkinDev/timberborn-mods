using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Editor.ModTools
{
  internal class EditorCreateMod : EditorWindow
  {
    private const string AuthorName = "Battery";
    
    [MenuItem("Timberborn/Create new mod", false, 1000000)]
    private static void CreateNewMod() => TextBoxPopup.ShowWindow(OnTextSubmitted);
    
    private static void OnTextSubmitted(string enteredText)
    {
      var modName = enteredText.Trim().Replace(" ", "");
      
#region Folders
      var modFolder = CreateFolder("Assets/Mods", modName);
      var assetFolder = CreateFolder(modFolder, $@"AssetBundles\Resources\{AuthorName}.{modName}");
      var dataFolder = CreateFolder(modFolder, "Data");
      var scriptsFolder = CreateFolder(modFolder, "Scripts");
#endregion
      
#region General
      CreateManifest(modFolder, $"{AuthorName}.{modName}", modName);
#endregion

#region Data
      var blueprintsFolder = CreateFolder(dataFolder,"Blueprints");
      var localizationsFolder = CreateFolder(dataFolder,"Localizations");
      CreateWorkshopData(dataFolder, modName);
      CreateThumbnail(dataFolder);
      CreateLocalizationFile(localizationsFolder);
#endregion
      
#region Scripts
      CreateAsmdef(
        folderPath: scriptsFolder, 
        rootNamespace: $"{AuthorName}.{modName}", 
        assemblyName: $"{AuthorName}.{modName}"
      );
      CreateModStarter(scriptsFolder, $"{AuthorName}.{modName}", modName);
#endregion

      AssetDatabase.Refresh();
    }
    
    private static void CreateThumbnail(string folderPath)
    {
      try
      {
        if (!Directory.Exists(folderPath))
        {
          throw new Exception($"Target folder does not exist: {folderPath}");
        }

        var original = Resources.Load<Texture2D>("ui/images/core/default-thumbnail");
        var outputPath = Path.Combine(folderPath, "thumbnail.png");
        if (!original)
        {
          Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new IOException("thumbnail.png File not found"));
        }
        else
        {
          var rt = RenderTexture.GetTemporary(original.width, original.height, 0);
          Graphics.Blit(original, rt);

          RenderTexture.active = rt;
          var copy = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
          copy.ReadPixels(new Rect(0, 0, original.width, original.height), 0, 0);
          copy.Apply();

          RenderTexture.active = null;
          RenderTexture.ReleaseTemporary(rt);
        
          var pngData = copy.EncodeToPNG();
          if (pngData != null)
          {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new IOException("thumbnail.png File not found"));
            File.WriteAllBytes(outputPath, pngData);
          }
          else
          {
            throw new Exception("Failed to encode PNG.");
          }

          DestroyImmediate(copy);
        }
        AssetDatabase.Refresh();
      }
      catch (Exception e)
      {
        Debug.LogError(e);
        throw;
      }
    }
    
    private static void CreateWorkshopData(string folderPath, string modName)
    {
      try
      {
        if (!Directory.Exists(folderPath))
        {
          throw new Exception($"Target folder does not exist: {folderPath}");
        }
      
        var workshopData = new
        {
          ItemId = "0000000000",
          Name = modName,
          Visibility = "Public",
          UpdateDescription = false,
          UpdateVisibility = false,
          UpdatePreview = true,
        };
      
        var json = JsonConvert.SerializeObject(workshopData, Formatting.Indented);
        var filePath = Path.Combine(folderPath, "workshop_data.json");

        File.WriteAllText(filePath, json);
        AssetDatabase.Refresh();
      }
      catch (Exception e)
      {
        Debug.LogError(e);
        throw;
      }
    }

    private static void CreateLocalizationFile(string folderPath)
    {
      try
      {
        var path = Path.Combine(folderPath, "enUS.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new IOException("Localization File not found"));

        using var writer = new StreamWriter(path, append: false);
        writer.WriteLine("ID,Text,Comment");
        writer.WriteLine("");
      }
      catch (Exception e)
      {
        Debug.LogError(e);
        throw;
      }
    }

    private static void CreateManifest(string folderPath, string rootNamespace, string modName)
    {
      try
      {
        if (!Directory.Exists(folderPath))
        {
          throw new Exception($"Target folder does not exist: {folderPath}");
        }

        var manifest = new
        {
          Name = modName,
          Version = "1.0.0.0",
          Id = rootNamespace,
          MinimumGameVersion = "0.0.0.0",
          Description = modName,
          RequiredMods = Array.Empty<object>(),
        };
      
        var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
        var filePath = Path.Combine(folderPath, "manifest.json");

        File.WriteAllText(filePath, json);
        AssetDatabase.Refresh();
      }
      catch (Exception e)
      {
        Debug.LogError(e);
        throw;
      }
    }

    private static void CreateModStarter(string folderPath, string rootNamespace, string modName)
    {
      try
      {
        if (!Directory.Exists(folderPath))
        {
          Directory.CreateDirectory(folderPath);
        }
      
        var path = Path.Combine(folderPath, "ModStarter.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new IOException("ModStarter File not found"));

        using var writer = new StreamWriter(path, append: false);
        writer.WriteLine("using Timberborn.ModManagerScene;");
        writer.WriteLine("using UnityEngine;");
        writer.WriteLine();
        writer.WriteLine($"namespace {rootNamespace}");
        writer.WriteLine("{");
        writer.WriteLine("  public class ModStarter : IModStarter");
        writer.WriteLine("  {");
        writer.WriteLine("    public void StartMod()");
        writer.WriteLine("    {");
        writer.WriteLine($"      Debug.Log(\"Starting {modName}\");");
        writer.WriteLine("    }");
        writer.WriteLine("  }");
        writer.WriteLine("}");
        AssetDatabase.Refresh();
      }
      catch (Exception e)
      {
        Debug.LogError(e);
        throw;
      }
    }

    private static void CreateAsmdef(
      string folderPath,
      string rootNamespace,
      string assemblyName,
      string[] references = null,
      string[] includePlatforms = null,
      string[] excludePlatforms = null,
      bool allowUnsafeCode = false,
      bool autoReferenced = true)
    {
      try
      {
        if (!Directory.Exists(folderPath))
        {
          throw new Exception($"Target folder does not exist: {folderPath}");
        }

        var asmdef = new
        {
          name = assemblyName,
          rootNamespace = rootNamespace,
          references = references ?? Array.Empty<string>(),
          includePlatforms = includePlatforms ?? Array.Empty<string>(),
          excludePlatforms = excludePlatforms ?? Array.Empty<string>(),
          allowUnsafeCode = allowUnsafeCode,
          autoReferenced = autoReferenced
        };

        var json = JsonConvert.SerializeObject(asmdef, Formatting.Indented);
        var filePath = Path.Combine(folderPath, assemblyName + ".asmdef");

        File.WriteAllText(filePath, json);
        AssetDatabase.Refresh();
      }
      catch (Exception e)
      {
        Debug.LogError(e);
        throw;
      }
    }
    
    private static string CreateFolder(string root, string relativePath)
    {
      try
      {
        var parts = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var current = root;

        foreach (var part in parts)
        {
          var next = System.IO.Path.Combine(current, part).Replace("\\", "/");

          if (!AssetDatabase.IsValidFolder(next))
          {
            AssetDatabase.CreateFolder(current, part);
          }

          current = next;
        }

        AssetDatabase.Refresh();
        return current;
      }
      catch (Exception e)
      {
        Debug.LogError(e);
        throw;
      }
    }
  }
}