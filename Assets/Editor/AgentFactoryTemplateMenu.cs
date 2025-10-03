using UnityEditor;
using UnityEngine;

public static class AgentFactoryTemplateMenu
{
    [MenuItem("Assets/Create/AIGame/Agent Factory")]
    static void CreateAgentFactory()
    {
        string templatePath = FindTemplateFile("90-AIGame__Agent Factory-NewAgentFactory.cs.txt");
        if (!string.IsNullOrEmpty(templatePath))
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewAgentFactory.cs");
        }
        else
        {
            Debug.LogError("Could not find Agent Factory template. Please ensure the AIGame template files are available in your project.");
        }
    }

    [MenuItem("Assets/Create/AIGame/AI Agent")]
    static void CreateAIAgent()
    {
        string templatePath = FindTemplateFile("91-AIGame__AI Agent-NewAIAgent.cs.txt");
        if (!string.IsNullOrEmpty(templatePath))
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewAIAgent.cs");
        }
        else
        {
            Debug.LogError("Could not find AI Agent template. Please ensure the AIGame template files are available in your project.");
        }
    }

    /// <summary>
    /// Dynamically finds template files in the project, checking multiple possible locations.
    /// </summary>
    /// <param name="templateFileName">The template file name to search for</param>
    /// <returns>The full path to the template file, or null if not found</returns>
    private static string FindTemplateFile(string templateFileName)
    {
        // Search for the template file using Unity's AssetDatabase
        string[] guids = AssetDatabase.FindAssets(templateFileName.Replace(".cs.txt", ""));

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(templateFileName))
            {
                return path;
            }
        }

        // Fallback: check common locations manually
        string[] possiblePaths = {
            "Assets/ScriptTemplates/" + templateFileName,
            "Assets/Scripts/Core/ScriptTemplates/" + templateFileName,
            "Packages/com.dania.aigame.core/ScriptTemplates/" + templateFileName
        };

        foreach (string path in possiblePaths)
        {
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(path) != null)
            {
                return path;
            }
        }

        return null;
    }
}
