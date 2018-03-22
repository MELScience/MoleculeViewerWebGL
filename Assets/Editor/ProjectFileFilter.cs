using System;
using System.Linq;
using UnityEditor;
using SyntaxTree.VisualStudio.Unity.Bridge;

/// <summary>
/// Unity Project generator extension, that filters out large amount of useless files.
/// Right now it filters all files under "Assets\MELChemistryVRData\Resources\MELDB\"
/// </summary>

[InitializeOnLoad]
public class ProjectFileFilter
{
    static ProjectFileFilter()
    {
        ProjectFilesGenerator.ProjectFileGeneration += (string name, string content) => {
            var nl = Environment.NewLine;
            // remove all MELDB stuff from project
            var lines = content.
                    Split(new[] { nl }, StringSplitOptions.None).
                    Where(o => !o.Contains("<None Include=\"Assets\\MELDB_xml\\")).
                    ToArray();

            return string.Join(nl, lines);
        };
    }
}