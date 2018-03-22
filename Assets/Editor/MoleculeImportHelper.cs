using UnityEngine;
using System.IO;
using System.Xml;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

public class MoleculeImportHelper
{
    private static Element MAX_CONSTRUCTOR_ELEMENT = Element.Ar;
    private static bool ProgressDialog(string title, int current, int total)
    { return EditorUtility.DisplayCancelableProgressBar(title, current + " / " + total, (float)current / total); }

    #region validation functions

    [MenuItem("Assets/Build/MelDB binary", validate = true)]
    [MenuItem("MELDB/Serialize to Binary", validate = true)]
    [MenuItem("MELDB/Refresh DB", validate = true)]
    [MenuItem("MELDB/Download missing 3D", validate = true)]
    [MenuItem("MELDB/Import missing 3D", validate = true)]
    [MenuItem("MELDB/Process Whole DB", validate = true)]
    [MenuItem("MELDB/Check duplicated structures", validate = true)]
    [MenuItem("MELDB/Import PubChem Particle", validate = true)]
    [MenuItem("MELDB/Import PubChem Particles from folder", validate = true)]
    [MenuItem("MELDB/Import PubChem Grabbed DB", validate = true)]
    [MenuItem("MELDB/Import One Particle from Grabbed Pubchem DB", validate = true)]
    private static bool ValidateMelDBMenuAvailability()
    {
        var xmlIndexPath = MelDBSerializerXML.GetIndexPath();

        Debug.Log(xmlIndexPath);

        return Directory.Exists(Directory.GetParent(xmlIndexPath).FullName) && File.Exists(xmlIndexPath);
    }

    #endregion

    [MenuItem("Assets/Build/MelDB binary")]
    [MenuItem("MELDB/Serialize to Binary", priority = 2000)]
    private static void serializeToBinary()
    {
        Clear();
        EditorUtility.DisplayProgressBar("Loading and sorting DB", "", 0f);
        MelDB.Init();

        MelDB.FilterOut(p => p.CASes.Count == 0);

        MelDBSerializer.binaryInstance.Serialize(MelDB.Instance, false, progress: ProgressDialog);
        EditorUtility.ClearProgressBar();
        Clear();
        AssetDatabase.Refresh();
    }
    [MenuItem("MELDB/Refresh DB", priority = 1999)]
    private static void refreshDb()
    {
        Clear();
        EditorUtility.DisplayProgressBar("Loading and sorting DB", "", 0f);
        var particles = MelDB.Particles;
        int totalParts = particles.Count;
        float particlesCount = particles.Count;
        string postfix = " / " + particles.Count;
        MelDB.Instance.Sort();
        for (int i = particles.Count - 1; i >= 0; i--) {
            if (EditorUtility.DisplayCancelableProgressBar("Refreshing DB", (totalParts - i) + postfix, (particlesCount - i) / particlesCount)) {
                EditorUtility.ClearProgressBar();
                Clear();
                return;
            }
            UpdateNameAndFlags(particles[i]);
        }
        MelDBSerializer.xmlInstance.Serialize(MelDB.Instance, false, progress: ProgressDialog);
        // stat about preferred names:
        var sbMany = new StringBuilder("Preferred names included MULTIPLE times:\n");
        bool hasMany = false;
        var sbNone = new StringBuilder("Preferred names was NEVER used:\n");
        bool hasNone = false;
        foreach (var pnf in _preferredNames.Values) {
            if (pnf.counter == 1)
                continue;
            if (pnf.counter == 0) {
                sbNone.AppendFormat("\t{0} \t\t({1})\n", pnf.name, pnf.formula);
                hasNone = true;
            } else {
                sbMany.AppendFormat("\t{0} \t\t({1}) \t\t - {2}\n", pnf.name, pnf.formula, pnf.counter);
                hasMany = true;
            }
        }
        if (hasMany)
            Debug.LogWarning(sbMany.ToString());
        if (hasNone)
            Debug.LogWarning(sbNone.ToString());
        // clean-up
        Clear();
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    [MenuItem("MELDB/Download missing 3D")]
    private static void downloadMissing3D()
    {
        string initialPath = Application.dataPath + "Assets/MELChemistryVRData/";
        string selectedFolder = EditorUtility.OpenFolderPanel("Select folder to store 3D files", initialPath, "");
        if (string.IsNullOrEmpty(selectedFolder))
            return;
        selectedFolder = selectedFolder.Replace('\\', '/');
        if (selectedFolder[selectedFolder.Length - 1] != '/')
            selectedFolder += '/';
        Clear();
        EditorUtility.DisplayProgressBar("Loading and sorting DB", "", 0f);
        var particles = MelDB.Particles;
        int totalParts = particles.Count;
        float particlesCount = particles.Count;
        string postfix = " / " + particles.Count;
        for (int i = particles.Count - 1; i >= 0; i--) {
            if (EditorUtility.DisplayCancelableProgressBar("Downloading missing 3D", (totalParts - i) + postfix, (particlesCount - i) / particlesCount)) {
                EditorUtility.ClearProgressBar();
                Clear();
                return;
            }
            var p = particles[i];
            if ((p.flags & ParticleInfo.ParticleFlags.Has3D) > 0)
                continue;
            if ((p.flags & ParticleInfo.ParticleFlags.HasTopology) == 0)
                continue;
            for (int cIndx = 0; cIndx < p.CASes.Count; cIndx++) {
                string cas = MelDB.CASToString(p.CASes[cIndx]);
                string path = selectedFolder + p.id + ".mol";
                string link = "http://webbook.nist.gov/cgi/cbook.cgi?Str3File=C" + cas;
                for (int attempt = 10; attempt > 0; attempt--) {
                    var client = new System.Net.WebClient();
                    try {
                        client.DownloadFile(link, path);
                    }
#pragma warning disable CS0168 // Variable is declared but never used
                    catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
                    {
                        client.Dispose();
                        continue;
                    }
                    client.Dispose();
                    break;
                }
                if (File.Exists(path) && File.ReadAllText(path).Length < 10)
                    File.Delete(path);
            }
        }
        EditorUtility.ClearProgressBar();
    }
    [MenuItem("MELDB/Import missing 3D")]
    private static void ImportMissing3D()
    {
        string initialPath = Application.dataPath + "Assets/MELChemistryVRData/";
        string selectedFolder = EditorUtility.OpenFolderPanel("Select folder to store 3D files", initialPath, "");
        if (string.IsNullOrEmpty(selectedFolder))
            return;
        selectedFolder = selectedFolder.Replace('\\', '/');
        if (selectedFolder[selectedFolder.Length - 1] != '/')
            selectedFolder += '/';
        Clear();
        EditorUtility.DisplayProgressBar("Loading and sorting DB", "", 0f);
        MelDB.Instance.Sort();

        var files = Directory.GetFiles(selectedFolder, "*.mol", SearchOption.TopDirectoryOnly);
        float progressFactor = 1f / files.Length;
        string postfix = " / " + files.Length;

        for (int i = 0; i < files.Length; i++) {
            var f = files[i];
            if (f.EndsWith(".meta"))
                continue;

            if (EditorUtility.DisplayCancelableProgressBar("Importing missing 3D", i + postfix, i * progressFactor)) {
                EditorUtility.ClearProgressBar();
                Clear();
                return;
            }
            ulong id = ulong.Parse(Path.GetFileNameWithoutExtension(f));
            var p = MelDB.FindParticleById(id);
            if (p == null) {
                Debug.LogWarningFormat("Particle with id {0} was not found in MelDB", id);
                continue;
            }
            if ((p.flags & ParticleInfo.ParticleFlags.Has3D) > 0)
                continue;
            if (!Import3DStructureFromMolFile(p, f, true)) {
                Debug.LogWarningFormat("Failed particle 3D import from file '{0}' for particle", f, id);
            }
            FindParticleBestRotation(p);
        }
        MelDBSerializer.xmlInstance.Serialize(MelDB.Instance, false, progress: ProgressDialog);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    [MenuItem("MELDB/Process Whole DB", priority = 1000)]
    private static void convertDB()
    {
        Clear();

        var particles = MelDB.Particles;
        float particlesCount = particles.Count;
        string postfix = " / " + particles.Count;
        MelDB.Instance.Sort();
        int totalParts = particles.Count;
        for (int i = particles.Count - 1; i >= 0; i--) {
            if (EditorUtility.DisplayCancelableProgressBar("Processing particles", (totalParts - i) + postfix, (particlesCount - i) / particlesCount)) {
                EditorUtility.ClearProgressBar();
                Clear();
                return;
            }

            var p = particles[i];

            p.primaryName = ParticleInfo.UnifiedName(p.primaryName);
            p.name = ParticleInfo.UnifiedName(p.name);
            for (int j = 0; j < p.iupacs.Count; j++)
                p.iupacs[j] = ParticleInfo.UnifiedName(p.iupacs[j]);

            //UpdateNameAndFlags(p);
            //UpdateHashes(p);
            //FindParticleBestRotation(p);
            //ParticleComparer.Test(p);

            //if (!string.IsNullOrEmpty(p.chemicalFormula))
            //    p.flags |= ParticleInfo.ParticleFlags.HasChemicalFormula;
        }
        MelDBSerializer.xmlInstance.Serialize(MelDB.Instance, true, false, progress: ProgressDialog);

        EditorUtility.ClearProgressBar();
        //AssetDatabase.Refresh();
    }
    private static void FindParticleBestRotation(ParticleInfo p)
    {
        if ((p.flags & ParticleInfo.ParticleFlags.Has2D) == 0
            || (p.flags & ParticleInfo.ParticleFlags.Has3D) == 0)
            return;
        var atoms = p.atoms;
        if (atoms.Count < 2)
            return;

        // find closest rotation of 3d atoms to fit 2d atoms positions
        float bestScore = float.PositiveInfinity;
        Quaternion best = Quaternion.identity;
        for (float x = -180f; x < 180f; x += 30f)
            for (float y = -180f; y < 180f; y += 30f)
                for (float z = -180f; z < 180f; z += 30f) {
                    float currentScore = 0f;
                    var q = Quaternion.Euler(x, y, z);
                    for (int i = atoms.Count - 1; i >= 0; i--) {
                        var atom = atoms[i];
                        var pos3d = q * atom.position;
                        currentScore += Vector3.Angle(pos3d, atom.flatPosition);
                    }
                    if (currentScore < bestScore) {
                        bestScore = currentScore;
                        best = q;
                    }
                }
        // fine-tuning
        for (float x = -6f; x < 6f; x += 1f)
            for (float y = -6f; y < 6f; y += 1f)
                for (float z = -6f; z < 6f; z += 1f) {
                    float currentScore = 0f;
                    var q = Quaternion.Euler(x, y, z) * best;
                    for (int i = atoms.Count - 1; i >= 0; i--) {
                        var atom = atoms[i];
                        var pos3d = q * atom.position;
                        currentScore += Vector3.Angle(pos3d, atom.flatPosition);
                    }
                    if (currentScore < bestScore) {
                        bestScore = currentScore;
                        best = q;
                    }
                }
        // decompose found best rotation into 2d structure rotation and 3d one (swing-twist quaternion decomposition across z-axis)
        Quaternion rotationFlat = Quaternion.identity;// Quaternion.Euler(0f, 0f, angles.z);
        {
            Vector2 invalid = new Vector3(float.PositiveInfinity, 0f, 0f);
            Vector2 firstAtom = invalid;
            Vector2 commonDirection = invalid;
            Vector2 firstNothAtom = invalid;
            Vector2 nothDirection = invalid;
            bool hFlat = true;
            for (int i = atoms.Count - 1; i >= 0; i--) {
                var atom = atoms[i];

                if (hFlat) {
                    if (firstAtom.x > 10000000f)
                        firstAtom = atom.flatPosition;
                    else if (commonDirection.x > 10000000f)
                        commonDirection = (atom.flatPosition - firstAtom).normalized;
                    else {
                        var d = (atom.flatPosition - firstAtom).normalized;
                        if (Mathf.Abs(Vector2.Dot(commonDirection, d)) < 0.9f) {
                            commonDirection = invalid;
                            hFlat = false;
                        }
                    }
                }

                if (atom.element != Element.H) {
                    if (firstNothAtom.x > 10000000f)
                        firstNothAtom = atom.flatPosition;
                    else if (nothDirection.x > 10000000f)
                        nothDirection = (atom.flatPosition - firstNothAtom).normalized;
                    else {
                        var d = (atom.flatPosition - firstNothAtom).normalized;
                        if (Mathf.Abs(Vector2.Dot(nothDirection, d)) < 0.9f) {
                            nothDirection = invalid;
                            break;
                        }
                    }
                }
            }
            if (nothDirection.x < 10000000f | (commonDirection.x < 10000000f & hFlat)) {
                float anglez = (hFlat & commonDirection.x < 100000000f)
                    ? Vector2.SignedAngle(commonDirection, new Vector2(1f, 0f))
                    : Vector2.SignedAngle(nothDirection, new Vector2(1f, 0f));
                if (anglez > 90f)
                    anglez -= 180f;
                if (anglez < -90f)
                    anglez += 180f;
                rotationFlat = Quaternion.Euler(0f, 0f, anglez);
            }
        }
        Quaternion rotation3d = rotationFlat * best;

        // find an appropriate scale for the 2d formula to fit into the space of 3d model
        float maxRadius3d = 1f;
        float maxRadius2d = 1f;
        for (int i = atoms.Count - 1; i >= 0; i--) {
            var r3 = atoms[i].position.sqrMagnitude;
            if (maxRadius3d < r3)
                maxRadius3d = r3;
            var r2 = atoms[i].flatPosition.sqrMagnitude;
            if (maxRadius2d < r2)
                maxRadius2d = r2;
        }
        maxRadius3d = Mathf.Sqrt(maxRadius3d) + 0.35f; // 0.35 because 3d atoms are bigger than 2d formula letters
        maxRadius2d = Mathf.Sqrt(maxRadius2d);
        float scale = maxRadius3d / maxRadius2d;
        if (scale < 1f) scale = 1f; // we don't want to shrink 2d view even more

        // process all atoms: rotate 3d structure, rotate and scale 2d structure
        for (int i = atoms.Count - 1; i >= 0; i--) {
            var atom = atoms[i];
            atom.position = rotation3d * atom.position;
            atom.flatPosition = scale * (rotationFlat * atom.flatPosition);
        }
    }

    [MenuItem("MELDB/Check duplicated structures", priority = 1001)]
    private static void checkDuplicatedStructuresDB()
    {
        var particles = MelDB.Particles;
        float particlesCount = particles.Count;

        var exact = new Dictionary<uint, string>();
        var approx = new Dictionary<uint, string>();

        var exact_coll = new Dictionary<uint, List<string>>(800);
        var approx_coll = new Dictionary<uint, List<string>>(800);

        for (int i = particles.Count - 1; i >= 0; i--) {
            EditorUtility.DisplayProgressBar("Processing particles", "", (particlesCount - i) / particlesCount);
            var p = particles[i];

            if (p.particleType != MelDB.DataType.Molecule)
                continue;

            if (exact.ContainsKey(p.structureHashExact)) {
                List<string> c;
                if (!exact_coll.TryGetValue(p.structureHashExact, out c)) {
                    c = new List<string>();
                    exact_coll.Add(p.structureHashExact, c);
                }
                c.Add(p.name);
            } else
                exact.Add(p.structureHashExact, p.name);

            if (approx.ContainsKey(p.structureHash)) {
                List<string> c;
                if (!approx_coll.TryGetValue(p.structureHash, out c)) {
                    c = new List<string>();
                    approx_coll.Add(p.structureHash, c);
                }
                c.Add(p.name);
            } else
                approx.Add(p.structureHash, p.name);
        }
        EditorUtility.ClearProgressBar();

        foreach (var kvp in exact_coll)
            Debug.LogErrorFormat("exact: {0}: {1}\n\t{3}\n\t{2}", kvp.Value.Count + 1, kvp.Key, string.Join("\n\t", kvp.Value.ToArray()), exact[kvp.Key]);
        foreach (var kvp in approx_coll)
            Debug.LogErrorFormat("approx: {0}: {1}\n\t{3}\n\t{2}", kvp.Value.Count + 1, kvp.Key, string.Join("\n\t", kvp.Value.ToArray()), approx[kvp.Key]);
    }

    private static string rawMELDB_datapath = "Assets/MELChemistryVRData/Resources/";
    private static string noNamePrefix = "Cid ";

    [MenuItem("MELDB/Import PubChem Particle", priority = 1)]
    public static void ImportPubChemParticle()
    {
        Clear();
        string initialPath = Application.dataPath + "Assets/MELChemistryVRData/PubChem_Particles/";
        string selectedFile = EditorUtility.OpenFilePanel("Select any pub-chem downloaded file", initialPath, "xml");
        if (string.IsNullOrEmpty(selectedFile))
            return;

        string partName = Path.GetFileName(selectedFile);
        string directory = Directory.GetParent(selectedFile).FullName;
        if (partName.StartsWith("Structure"))
            partName = partName.Substring("Structure2D_".Length);
        if (partName.StartsWith("CID_"))
            partName = partName.Substring("CID_".Length);

        ImportOnePubChemParticle(
            directory + "/CID_",
            directory + "/Structure2D_CID_",
            directory + "/Structure3D_CID_",
            partName, true, true);

        MelDBSerializer.xmlInstance.Serialize(MelDB.Instance, false, progress: ProgressDialog);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    [MenuItem("MELDB/Import PubChem Particles from folder", priority = 1)]
    public static void ImportPubChemParticleFromFolder()
    {
        Clear();
        string initialPath = Application.dataPath + "Assets/MELChemistryVRData/PubChem_Particles/";
        string selectedFile = EditorUtility.OpenFilePanel("Select any pub-chem downloaded file from folder", initialPath, "xml");
        if (string.IsNullOrEmpty(selectedFile))
            return;

        string directory = Directory.GetParent(selectedFile).FullName;
        string cidDir = directory + "/CID_";
        string dir2d = directory + "/Structure2D_CID_";
        string dir3d = directory + "/Structure3D_CID_";

        var files = Directory.GetFiles(directory, "CID_*.xml", SearchOption.TopDirectoryOnly);

        var errors = new List<string>[(int)ImportResult.MAX];
        for (int i = 0; i <= (int)ImportResult.MAX - 1; i++)
            errors[i] = new List<string>();

        int filesCount = files.Length;
        for (int i = 0; i < filesCount; i++) {
            var file = files[i];
            EditorUtility.DisplayProgressBar("Imorting Particle DataBase", file, (float)i / filesCount);
            var fileName = Path.GetFileName(file);
            if (!fileName.StartsWith("CID_"))
                continue;
            string partName = fileName.Substring(4); // ignore "CID_"
            var result = ImportOnePubChemParticle(cidDir, dir2d, dir3d, partName, false, true);
            errors[(int)result].Add(partName);
        }
        EditorUtility.ClearProgressBar();
        files = null;

        PrintResults(filesCount, errors);

        MelDBSerializer.xmlInstance.Serialize(MelDB.Instance, false, progress: ProgressDialog);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    [MenuItem("MELDB/Import PubChem Grabbed DB", priority = 2)]
    public static void ImportGrabbedPubGhemDatabase()
    {
        Clear();

        string initialPath = Application.dataPath;
        string selectedFile = EditorUtility.OpenFilePanel("Select any pub-chem description file (CID)", initialPath, "xml");

        string directory = Directory.GetParent(selectedFile).FullName;
        var files = Directory.GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly);

        string cidDir = directory + "/CID_";
        string dir2d = Directory.GetParent(directory).FullName + "/xml_structure/2D";
        string dir3d = Directory.GetParent(directory).FullName + "/xml_structure/3D";

        var errors = new List<string>[(int)ImportResult.MAX];
        for (int i = 0; i <= (int)ImportResult.MAX - 1; i++)
            errors[i] = new List<string>();

        int filesCount = files.Length;
        for (int i = 0; i < filesCount; i++) {
            var file = files[i];
            EditorUtility.DisplayProgressBar("Imorting Particle DataBase", file, (float)i / filesCount);
            string partName = Path.GetFileName(file).Substring(4); // ignore "CID_"
            var result = ImportOnePubChemParticle(cidDir, dir2d, dir3d, partName, false, false);
            errors[(int)result].Add(partName);
        }
        EditorUtility.ClearProgressBar();
        files = null;

        PrintResults(filesCount, errors);

        MelDBSerializer.xmlInstance.Serialize(MelDB.Instance, false, progress: ProgressDialog);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }
    private static void PrintResults(int filesCount, List<string>[] errors)
    {
        var sb = new StringBuilder(10000);
        sb.Append("Total files - ");
        sb.AppendLine(filesCount.ToString());

        Action<ImportResult, bool> debug = (i, showNames) => {
            int count = errors[(int)i].Count;
            if (count == 0)
                return;
            sb.Append(i.ToString().ToUpper());
            sb.Append(" - ");
            sb.AppendLine(count.ToString());
            if (showNames) {
                var list = errors[(int)i];
                foreach (var f in list) {
                    sb.Append("\t");
                    sb.AppendLine(f);
                }
            }
            sb.AppendLine();
        };

        bool hasWarnings = errors[(int)ImportResult.SuccessAutofixed].Count > 0;
        if (hasWarnings)
            sb.Append("WARNINGS! check the autofixed files\n");

        debug(ImportResult.Success, false);
        debug(ImportResult.SuccessAutofixed, true);
        debug(ImportResult.TopologyMismatch, true);
        debug(ImportResult.No2dAnd3d, true);
        debug(ImportResult.InvalidCID, true);
        debug(ImportResult.Unparsable2d, true);
        debug(ImportResult.Unparsable3d, true);
        debug(ImportResult.NoNames, false);
        debug(ImportResult.NoPrimaryName, false);
        debug(ImportResult.Collision, false);
        debug(ImportResult.ForbiddenElements, false);
        debug(ImportResult.ForcedRadicalsOrCharges, true);

        if (hasWarnings)
            Debug.LogWarning(sb.ToString());
        else
            Debug.Log(sb.ToString());
    }

    [MenuItem("MELDB/Import One Particle from Grabbed Pubchem DB", priority = 3)]
    public static void ImportOneParticleFromGrabbedPubChemDatabase()
    {
        Clear();
        string initialPath = Application.dataPath;
        string selectedFile = EditorUtility.OpenFilePanel("Select pub-chem description file (CID)", initialPath, "xml");

        string directory = Directory.GetParent(selectedFile).FullName;

        string cidDir = directory + "/CID_";
        string dir2d = Directory.GetParent(directory).FullName + "/xml_structure/2D";
        string dir3d = Directory.GetParent(directory).FullName + "/xml_structure/3D";

        string partName = Path.GetFileName(selectedFile).Substring(4); // ignore "CID_"
        var result = ImportOnePubChemParticle(cidDir, dir2d, dir3d, partName, true, true);

        if (result == ImportResult.Success)
            Debug.Log("Imported: " + result + "\n" + selectedFile);
        else if (result == ImportResult.SuccessAutofixed)
            Debug.LogWarning("Imported: " + result + "\n" + selectedFile);
        else
            Debug.LogError("Imported: " + result + "\n" + selectedFile);

        MelDBSerializer.xmlInstance.Serialize(MelDB.Instance, false, progress: ProgressDialog);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    private static List<int> reorderings = new List<int>();
    private enum ImportResult : int
    {
        Success = 0,
        SuccessAutofixed,
        SuccessCancelled,
        InvalidCID,
        No2dAnd3d,
        Unparsable2d,
        Unparsable3d,
        TopologyMismatch,
        NoNames,
        NoPrimaryName,
        Collision,
        ForbiddenElements,
        ForcedRadicalsOrCharges,
        MAX
    }

    #region PubChem importer

    private static ImportResult ImportOnePubChemParticle(string cidDirectory, string directory2d, string directory3d, string partName, bool verbose, bool forceIncludeInBuild)
    {
        string part2dPath = directory2d + partName;
        string part3dPath = directory3d + partName;
        bool exist2d = File.Exists(part2dPath);
        bool exist3d = File.Exists(part3dPath);
        if (!exist2d && !exist3d) {
            if (verbose)
                EditorUtility.DisplayDialog("Data missing", "No 3D structure found, file\n" + part3dPath + "\ndoes not exist.\nParticle will not be imported.", "Ignore particle");
            return ImportResult.No2dAnd3d;
        }

        var particleInfo = new ParticleInfo(true);
        var cid = partName.Substring(0, partName.Length - 4); // trim ".xml"
        if (!uint.TryParse(cid, out particleInfo.CID)) {
            if (verbose)
                EditorUtility.DisplayDialog("Incorrect file name", "Particle CID can not be parsed from '" + cid + "'\nParticle will not be imported.", "Ignore particle");
            return ImportResult.InvalidCID;
        }

        string partInfoPath = cidDirectory + partName;
        if (File.Exists(partInfoPath)) {
            var xmli = ReadXmlFile(partInfoPath);
            ReadPubChemPrimaryIdentifyingName(xmli, particleInfo);
            ReadCASNumbersFromPubChem(xmli, particleInfo);
        } else {
            Debug.LogWarningFormat("Info file '{0}' was not found\n", partInfoPath);
        }

        bool nameIsSecure = !string.IsNullOrEmpty(particleInfo.primaryName);
        if (!nameIsSecure && !verbose)
            return ImportResult.NoPrimaryName;

        bool structresExactlyMatch = true;

        if (exist2d) {
            var xml = ReadXmlFile(part2dPath);
            var pi2d = new ParticleInfo(true);
            if (!ReadPubChemAtomsAndBonds(xml, pi2d, verbose)) {
                if (verbose)
                    EditorUtility.DisplayDialog(partName + " - non-parsable 2D atoms", "Particle '" + partName + "' contains atoms which are not contained in " + typeof(Element).FullName + " enumerator and cannot be imported.", "Ignore particle");
                return ImportResult.Unparsable2d;
            }
            ReadPubChemAtomPositions(xml, pi2d, true);
            ReadPubChemNames(xml, pi2d);
            particleInfo.TakeAbsentDataFrom(pi2d, reorderings);
        }

        if (exist3d) {
            var pi3d = new ParticleInfo(true);
            var xml = ReadXmlFile(part3dPath);
            if (!ReadPubChemAtomsAndBonds(xml, pi3d, verbose)) {
                if (verbose)
                    EditorUtility.DisplayDialog(partName + " - non-parsable 3D atoms", "Particle '" + partName + "' contains atoms which are not contained in " + typeof(Element).FullName + " enumerator and cannot be imported.", "Ignore particle");
                return ImportResult.Unparsable3d;
            }
            ReadPubChemAtomPositions(xml, pi3d, false);
            ReadPubChemNames(xml, pi3d);

            structresExactlyMatch = particleInfo.TakeAbsentDataFrom(pi3d, reorderings);

            if (!structresExactlyMatch) {
                if (verbose && !EditorUtility.DisplayDialog(partName + " - conflicting 2d and 3d topology", "Particle '" + partName + "' has different topology in 2d and 3d files.", "Continue (Use 2D)", "Ignore particle"))
                    return ImportResult.TopologyMismatch;
                if (!verbose)
                    return ImportResult.TopologyMismatch; // with high probability it's a resonance structure, we don't want to import it
                structresExactlyMatch = particleInfo.TakeAbsentDataFrom(pi3d, reorderings, true);
                if (!structresExactlyMatch) {
                    if (verbose)
                        EditorUtility.DisplayDialog(partName + " - completely conflicting 2d and 3d topology", "Particle '" + partName + "' has VERY different topology in 2d and 3d files.", "Ignore particle");
                    return ImportResult.TopologyMismatch;
                }
            }
        }

        if (particleInfo.atoms.Count == 1)
            particleInfo.flags &= ParticleInfo.ParticleFlags.HasTopology | ParticleInfo.ParticleFlags.Has3D | ParticleInfo.ParticleFlags.Has2D;

        if ((particleInfo.flags & ParticleInfo.ParticleFlags.HasTopology) == 0)
            return ImportResult.No2dAnd3d;

        FindBestName(particleInfo);

        if (string.IsNullOrEmpty(particleInfo.name)) {
            if (verbose)
                EditorUtility.DisplayDialog("No particle name found", "Cannot find any name in Info/2D/3D files", "Abort");
            return ImportResult.NoNames;
        }
        if (verbose & !nameIsSecure && !EditorUtility.DisplayDialog(particleInfo.name + " - name confirmation", "Primary indentifier name was not found, the name was selected as shortest from 3D/2D structures\n" + particleInfo.name + "\nCorrect?", "Yes, continue", "No, abort"))
            return ImportResult.NoPrimaryName;

        bool hasRadicalOrCharges = (particleInfo.flags & ParticleInfo.ParticleFlags.HasRadicalAtoms) > 0 || particleInfo.charge != 0;
        if (forceIncludeInBuild) {
            if (hasRadicalOrCharges) {
                Debug.LogErrorFormat("Trying to force add particle with radicals or non-0-charge\nName: {0}\n{1}\n", particleInfo.name, particleInfo.CID);
                return ImportResult.ForcedRadicalsOrCharges;
            }
            particleInfo.flags |= ParticleInfo.ParticleFlags.ForceIncludeInBuild;
        }
        if (hasRadicalOrCharges) {
            particleInfo.iupacs.Clear(); // assume incorrect IUPAC notation for radicals in PubChem
            FindBestName(particleInfo);
            if (string.IsNullOrEmpty(particleInfo.name)) {
                if (verbose)
                    EditorUtility.DisplayDialog("Radical or charge w/o primary name", "Cannot find primary name for particle " + particleInfo.CID + " assuming IUPACs are not valid from PubChem", "Abort");
                return ImportResult.NoNames;
            }
        }

        UpdateHashes(particleInfo);

        var collision = MelDB.Particles.FirstOrDefault(p =>
            p.CID == particleInfo.CID || (p.HashesEqual(particleInfo) && p.NamesEqual(particleInfo)));
        if (collision != null) {
            if (verbose && !EditorUtility.DisplayDialog("Success but collision! (" + particleInfo.name + ")", "MELDB already contains particle with id '" + collision.id + "' and name '" + collision.name + "'.\nDo you want to overwrite it?", "Try merge", "Ignore imported"))
                return ImportResult.Collision;
            if (forceIncludeInBuild)
                collision.primaryName = particleInfo.primaryName;
            if (!collision.TakeAbsentDataFrom(particleInfo, reorderings, true)) {
                if (verbose && !EditorUtility.DisplayDialog("Failed merge (topology)", "Cannot merge imported and existing particles - very different topology", "Overwrite existing", "Ignore imported"))
                    return ImportResult.Collision;
                MelDB.RemoveParticle(collision);
                MelDB.Particles.Add(particleInfo);
            } else {
                particleInfo = collision;
                FindBestName(particleInfo);
            }
        } else {
            if (verbose && !EditorUtility.DisplayDialog("Success", particleInfo.name + " - imported successfully, no collisions.\nProceed?", "Ok", "Cancel"))
                return ImportResult.SuccessCancelled;
            MelDB.Particles.Add(particleInfo);
        }

        FindParticleBestRotation(particleInfo);
        UpdateSkeletalFormulaFlag(particleInfo);
        UpdatePrimarilyFlatFlag(particleInfo);
        UpdateMolecularFormulaRuleAndCheckValence(particleInfo);
        UpdateVisibilityFlags(particleInfo);

        return structresExactlyMatch ? ImportResult.Success : ImportResult.SuccessAutofixed;
    }

    private static bool ReadPubChemAtomsAndBonds(XmlDocument xmlDoc, ParticleInfo particleInfo, bool verbose)
    {

        XmlNodeList atomNumbers = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_atoms/PC-Atoms/PC-Atoms_aid/PC-Atoms_aid_E");
        XmlNodeList atomTypes = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_atoms/PC-Atoms/PC-Atoms_element/PC-Element");

        //looking at .../PC-Atom_charge
        XmlNodeList chargedAtomNumber = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_atoms/PC-Atoms/PC-Atoms_charge/PC-AtomInt/PC-AtomInt_aid");
        XmlNodeList chargedAtomCharge = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_atoms/PC-Atoms/PC-Atoms_charge/PC-AtomInt/PC-AtomInt_value");
        XmlNodeList radicalAtomsNumber = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_atoms/PC-Atoms/PC-Atoms_radical/PC-AtomRadical/PC-AtomRadical_aid");
        XmlNodeList radicalAtomsCharge = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_atoms/PC-Atoms/PC-Atoms_radical/PC-AtomRadical/PC-AtomRadical_aid");

        var atoms = new AtomInfo[atomNumbers.Count];
        var atomN = new int[atomNumbers.Count];

        short sumCharges = 0;
        bool hasChargedAtoms = false;
        bool hasRadicalAtoms = false;

        for (int i = 0; i < atomNumbers.Count; i++) {
            int atomNumber = int.Parse(atomNumbers[i].InnerText);
            atomN[i] = atomNumber;

            AtomInfo atomInfo = new AtomInfo();

            atomInfo.atomCharge = 0;
            for (int j = 0; j < chargedAtomNumber.Count; j++) {
                if (atomNumber == int.Parse(chargedAtomNumber[j].InnerText)) {
                    atomInfo.atomCharge = sbyte.Parse(chargedAtomCharge[j].InnerText);
                    sumCharges += atomInfo.atomCharge;
                    hasChargedAtoms |= atomInfo.atomCharge != 0;
                }
            }
            atomInfo.radical = 0;
            for (int j = 0; j < radicalAtomsNumber.Count; j++) {
                if (atomNumber == int.Parse(radicalAtomsNumber[j].InnerText)) {
                    atomInfo.radical = sbyte.Parse(radicalAtomsCharge[j].InnerText);
                    hasRadicalAtoms |= atomInfo.radical != 0;
                }
            }

            string atomName = atomTypes[i].Attributes["value"].Value;
            try {
                atomInfo.element = (Element)Enum.Parse(typeof(Element), atomName, true);
            } catch {
                if (verbose)
                    Debug.LogError("Failed to parse element type " + atomName);
                return false;
            }
            atoms[i] = atomInfo;
        }

        particleInfo.charge = sumCharges;
        if (sumCharges != 0)
            particleInfo.flags |= ParticleInfo.ParticleFlags.HasParticleCharge;
        if (hasChargedAtoms)
            particleInfo.flags |= ParticleInfo.ParticleFlags.HasAtomCharges;
        if (hasRadicalAtoms)
            particleInfo.flags |= ParticleInfo.ParticleFlags.HasRadicalAtoms;

        Array.Sort(atomN, atoms);

        particleInfo.data.atoms = atoms.ToList();

        // read bonds

        XmlNodeList atom1Numbers = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_bonds/PC-Bonds/PC-Bonds_aid1/PC-Bonds_aid1_E");
        XmlNodeList atom2Numbers = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_bonds/PC-Bonds/PC-Bonds_aid2/PC-Bonds_aid2_E");
        XmlNodeList bondTypes = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_bonds/PC-Bonds/PC-Bonds_order/PC-BondType");

        var bonds = particleInfo.data.bonds;

        for (int i = 0; i < atom1Numbers.Count; i++) {
            short atom1Number = (short)(int.Parse(atom1Numbers[i].InnerText) - 1);
            short atom2Number = (short)(int.Parse(atom2Numbers[i].InnerText) - 1);

            BondInfo bondInfo = new BondInfo();
            bondInfo.atom1 = atom1Number;
            bondInfo.atom2 = atom2Number;

            try {
                bondInfo.bondType = (BondInfo.BondType)Enum.Parse(typeof(BondInfo.BondType), bondTypes[i].Attributes["value"].Value.ToUpper());
            } catch {
                if (verbose)
                    Debug.LogError("Failed to parse bond type " + bondTypes[i].Attributes["value"].Value.ToUpper());
                return false;
            }

            bonds.Add(bondInfo);
        }

        particleInfo.flags |= ParticleInfo.ParticleFlags.HasTopology;
        return true;
    }

    private static void ReadPubChemAtomPositions(XmlDocument xmlDoc, ParticleInfo particleInfo, bool twoD)
    {
        // Read atom coords.
        XmlNodeList atomNumbers = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_coords/PC-Coordinates/PC-Coordinates_aid/PC-Coordinates_aid_E");
        XmlNodeList atomCoordsX = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_coords/PC-Coordinates/PC-Coordinates_conformers/PC-Conformer/PC-Conformer_x/PC-Conformer_x_E");
        XmlNodeList atomCoordsY = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_coords/PC-Coordinates/PC-Coordinates_conformers/PC-Conformer/PC-Conformer_y/PC-Conformer_y_E");
        XmlNodeList atomCoordsZ = twoD ? null : xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_coords/PC-Coordinates/PC-Coordinates_conformers/PC-Conformer/PC-Conformer_z/PC-Conformer_z_E");

        Vector3 centerMass = Vector3.zero;
        var atoms = particleInfo.data.atoms;

        for (int i = 0; i < atomNumbers.Count; i++) {
            int atomNumber = int.Parse(atomNumbers[i].InnerText);
            var atom = atoms[atomNumber - 1];

            Vector3 position = new Vector3();
            position.x = float.Parse(atomCoordsX[i].InnerText);
            position.y = float.Parse(atomCoordsY[i].InnerText);
            if (twoD) {
                atom.flatPosition = position;
            } else {
                position.z = float.Parse(atomCoordsZ[i].InnerText);
                atom.position = position;
            }
            centerMass += position;
        }

        centerMass /= atomNumbers.Count;
        var centerMass2d = new Vector2(centerMass.x, centerMass.y);
        foreach (AtomInfo atomInfo in atoms) {
            if (twoD)
                atomInfo.flatPosition -= centerMass2d;
            else
                atomInfo.position -= centerMass;
        }
        if (twoD)
            particleInfo.flags |= ParticleInfo.ParticleFlags.Has2D;
        else
            particleInfo.flags |= ParticleInfo.ParticleFlags.Has3D;
    }

    private static void ReadPubChemNames(XmlDocument xmlDoc, ParticleInfo particle)
    {
        XmlNodeList urnLabels = xmlDoc.SelectNodes("/PC-Compounds/PC-Compound/PC-Compound_props/PC-InfoData/PC-InfoData_urn/PC-Urn/PC-Urn_label");
        for (int i = 0; i < urnLabels.Count; i++) {
            var urnLabel = urnLabels[i];
            if (!urnLabel.InnerText.Equals("IUPAC Name", StringComparison.Ordinal))
                continue;
            var val = FindChild(urnLabel.ParentNode.ParentNode.ParentNode, "PC-InfoData_value");
            if (val == null)
                continue;
            var valString = FindChild(val, "PC-InfoData_value_sval");
            if (valString == null)
                continue;
            var name = ParticleInfo.UnifiedName(valString.InnerText);
            if (name.StartsWith(noNamePrefix))
                continue;
            if (name.Equals(particle.primaryName, StringComparison.Ordinal))
                continue;
            if (particle.iupacs.Contains(name))
                continue;
            particle.iupacs.Add(name);
        }
    }

    private static void ReadPubChemPrimaryIdentifyingName(XmlDocument xmlDoc, ParticleInfo particle)
    {
        XmlNodeList infos = xmlDoc.SelectNodes("/Record/Section/Section/Description");
        for (int i = 0; i < infos.Count; i++) {
            var info = infos[i];
            if (!info.InnerText.Equals("Primary Identifying Name", StringComparison.Ordinal))
                continue;
            var information = FindChild(info.ParentNode, "Information");
            var val = FindChild(information, "StringValue");
            var name = ParticleInfo.UnifiedName(val.InnerText);
            if (name.StartsWith(noNamePrefix))
                continue;
            particle.primaryName = name;
            particle.iupacs.Remove(name);
            break;
        }
        XmlNodeList urnLabels = xmlDoc.SelectNodes("/Record/Section/Section/Section/Information/Name");
        for (int i = 0; i < urnLabels.Count; i++) {
            var urnLabel = urnLabels[i];
            if (!urnLabel.InnerText.Equals("IUPAC Name", StringComparison.Ordinal))
                continue;
            var val = FindChild(urnLabel.ParentNode, "StringValue");
            if (val == null)
                continue;
            var name = ParticleInfo.UnifiedName(val.InnerText);
            if (name.StartsWith(noNamePrefix))
                continue;
            if (name.Equals(particle.primaryName, StringComparison.Ordinal))
                continue;
            if (particle.iupacs.Contains(name))
                continue;
            particle.iupacs.Add(name);
        }
    }

    private static void ReadCASNumbersFromPubChem(XmlDocument xmlDoc, ParticleInfo particle)
    {
        XmlNodeList infos = xmlDoc.SelectNodes("/Record/Section/Section/Section/Information/Name");
        for (int i = 0; i < infos.Count; i++) {
            var info = infos[i];
            if (!info.InnerText.Equals("CAS", StringComparison.Ordinal))
                continue;
            var casString = FindChild(info.ParentNode, "StringValue").InnerText;
            var cas = MelDB.ParseCASNumber(casString);
            if (!particle.CASes.Contains(cas))
                particle.CASes.Add(cas);
        }
    }

    #endregion

    #region .mol and sdf importer

    private static char[] _separators = new char[] { ' ' };
    private static bool Import3DStructureFromMolFile(ParticleInfo p, string filename, bool verbose)
    {
        // http://www.nonlinear.com/progenesis/sdf-studio/v0.9/faq/sdf-file-format-guidance.aspx
        if (!File.Exists(filename)) {
            if (verbose)
                Debug.LogErrorFormat("File {0} was not found", filename);
            return false;
        }
        var lines = File.ReadAllLines(filename);
        if (lines.Length < 3) {
            if (verbose)
                Debug.LogErrorFormat("File {0} is too short", filename);
            return false;
        }
        int indx = 2;
        if (!lines[indx].Contains(" V2000") && !lines[indx].Contains(" v2000"))
            indx++;
        if (!lines[indx].Contains(" V2000") && !lines[indx].Contains(" v2000")) {
            if (verbose)
                Debug.LogErrorFormat("Counts line was not found in file {0}", filename);
            return false;
        }
        var values = lines[indx++].Split(_separators, StringSplitOptions.RemoveEmptyEntries);
        int atomsCount;
        int bondsCount;
        if (!int.TryParse(values[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out atomsCount)
            || !int.TryParse(values[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out bondsCount)) {
            if (verbose)
                Debug.LogErrorFormat("Unparsable counts line in file {0}", filename);
            return false;
        }
        if (atomsCount != p.atoms.Count || bondsCount != p.bonds.Count) {
            if (verbose)
                Debug.LogErrorFormat("Atoms/bonds counts from file {0} differs from the ones from particle {1} ({2})", filename, p.id, p.name);
            return false;
        }
        var newP = new ParticleInfo(true);
        for (int i = 0; i < atomsCount; i++) {
            var a = ParseMolAtom(lines[indx++], verbose);
            if (a == null) {
                if (verbose)
                    Debug.LogErrorFormat("Can not parse atom {0} in file {1}", i + 1, filename);
                return false;
            }
            newP.atoms.Add(a);
        }
        // move to center of mass
        Vector3 center = new Vector3();
        for (int i = 0; i < atomsCount; i++)
            center += newP.atoms[i].position;
        center /= atomsCount;
        for (int i = 0; i < atomsCount; i++)
            newP.atoms[i].position -= center;

        for (int i = 0; i < bondsCount; i++) {
            var b = ParseMolBond(lines[indx++], verbose);
            if (b == null) {
                if (verbose)
                    Debug.LogErrorFormat("Can not parse bond {0} in file {1}", i + 1, filename);
                return false;
            }
            newP.bonds.Add(b);
        }
        newP.flags |= ParticleInfo.ParticleFlags.HasTopology | ParticleInfo.ParticleFlags.Has3D;
        if (!p.TakeAbsentDataFrom(newP, reorderings, true)) {
            if (verbose)
                Debug.LogErrorFormat("Topology in file {0} differs from topology in particle {1} ({2})", filename, p.id, p.name);
            return false;
        }
        return true;
    }
    private static AtomInfo ParseMolAtom(string s, bool verbose)
    {
        var values = s.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
        var atom = new AtomInfo();
        if (!float.TryParse(values[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out atom.position.x))
            return null;
        if (!float.TryParse(values[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out atom.position.y))
            return null;
        if (!float.TryParse(values[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out atom.position.z))
            return null;
        try { atom.element = (Element)Enum.Parse(typeof(Element), values[3], true); } catch { return null; }
        int chargeRadical;
        if (!int.TryParse(values[5], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out chargeRadical))
            return null;
        switch (chargeRadical) {
            case 7:
                atom.atomCharge = -3;
                break;
            case 6:
                atom.atomCharge = -2;
                break;
            case 5:
                atom.atomCharge = -1;
                break;
            case 0:
                // no charges/radicals
                break;
            case 3:
                atom.atomCharge = 1;
                break;
            case 2:
                atom.atomCharge = 2;
                break;
            case 1:
                atom.atomCharge = 3;
                break;
            case 4:
                atom.radical = 1;
                break;
            default:
                if (verbose)
                    Debug.LogErrorFormat("Unparsable charge/radical for atom {0}: value {1}, expected 0-7", atom.element, chargeRadical);
                return null;
        }
        return atom;
    }
    private static BondInfo ParseMolBond(string s, bool verbose)
    {
        var values = s.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
        var bond = new BondInfo();
        if (!short.TryParse(values[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out bond.atom1))
            return null;
        if (!short.TryParse(values[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out bond.atom2))
            return null;
        bond.atom1--;
        bond.atom2--;
        int type;
        if (!int.TryParse(values[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out type))
            return null;
        switch (type) {
            case 1:
                bond.bondType = BondInfo.BondType.SINGLE;
                break;
            case 2:
                bond.bondType = BondInfo.BondType.DOUBLE;
                break;
            case 3:
                bond.bondType = BondInfo.BondType.TRIPLE;
                break;
            case 4:
                bond.bondType = BondInfo.BondType.SINGLEANDDASHED; // aromatic
                break;
            case 5:
                bond.bondType = BondInfo.BondType.SINGLEANDDASHED; // single or double
                break;
            case 6:
                bond.bondType = BondInfo.BondType.SINGLEANDDASHED; // single or aromatic
                break;
            case 7:
                bond.bondType = BondInfo.BondType.DOUBLE; // double or aromatic
                break;
            default:
                if (verbose)
                    Debug.LogErrorFormat("Unparsable or unknown bond type: value {0}, expected 1-7", type);
                return null;
        }
        return bond;
    }

    #endregion

    private static void UpdateNameAndFlags(ParticleInfo particle)
    {
        FindBestName(particle);
        UpdateChargeAndRadicalsFlags(particle);
        UpdateSkeletalFormulaFlag(particle);
        UpdatePrimarilyFlatFlag(particle);
        UpdateMolecularFormulaRuleAndCheckValence(particle);
        // chemical formula
        if (string.IsNullOrEmpty(particle.chemicalFormula))
            particle.flags &= ~ParticleInfo.ParticleFlags.HasChemicalFormula;
        else
            particle.flags |= ParticleInfo.ParticleFlags.HasChemicalFormula;
        // 2d and 3d for one-atom particles
        if (particle.atoms.Count == 1) {
            particle.atoms[0].position = new Vector3();
            particle.atoms[0].flatPosition = new Vector2();
            particle.flags |= ParticleInfo.ParticleFlags.Has3D | ParticleInfo.ParticleFlags.Has2D;
        }
        UpdateVisibilityFlags(particle);
    }

    private static List<Element> _uniqueElements = new List<Element>(8);
    private static HashSet<string> _molecularOrderExceptions = null;
    private enum MolecularFormulaType { Unknown, Organic, Acid, Normal, Reversed }
    private static int[] _connections = new int[300]; // buffer to speed up calculations
    private static void UpdateMolecularFormulaRuleAndCheckValence(ParticleInfo particle)
    {
        if (_molecularOrderExceptions == null)
            _molecularOrderExceptions = ReadLinesFromSourcesFile("ElectonegativityExceptions", false);

        var type = MolecularFormulaType.Unknown;
        ParticleInfo.GetUniqueAtoms(particle.atoms, _uniqueElements);

        if (_uniqueElements.Count < 2)
            type = MolecularFormulaType.Normal;

        if (type == MolecularFormulaType.Unknown && _uniqueElements.Count == 2) {
            if (_uniqueElements.Contains(Element.C) && _uniqueElements.Contains(Element.H))
                type = MolecularFormulaType.Organic;
            else {
                string name = _uniqueElements[0].ToString() + _uniqueElements[1].ToString();
                type = _molecularOrderExceptions.Contains(name) ? MolecularFormulaType.Reversed : MolecularFormulaType.Normal;
            }
        }

        if (type == MolecularFormulaType.Unknown && _uniqueElements.Count == 3 &&
                _uniqueElements.Contains(Element.O) & _uniqueElements.Contains(Element.H)) {
            // check if it's an acid with only one non-H-non-O atom
            var thirdElement = _uniqueElements[0];
            if (thirdElement == Element.O || thirdElement == Element.H) thirdElement = _uniqueElements[1];
            if (thirdElement == Element.O || thirdElement == Element.H) thirdElement = _uniqueElements[2];
            int count = 0;
            var atoms = particle.atoms;
            for (int i = 0; i < atoms.Count; i++)
                if (atoms[i].element == thirdElement)
                    count++;
            if (count == 1)
                type = MolecularFormulaType.Acid;
        }

        if (type == MolecularFormulaType.Unknown && _uniqueElements.Contains(Element.C) & _uniqueElements.Contains(Element.H)) {
            // search for C-H bond to treat as organic
            if (particle.ContainsBondedAtoms(Element.C, Element.H))
                type = MolecularFormulaType.Organic;
        }

        if (type == MolecularFormulaType.Unknown && _uniqueElements.Contains(Element.C)) {
            // search for C-C bond to treat as organic
            if (particle.ContainsBondedAtoms(Element.C, Element.C))
                type = MolecularFormulaType.Organic;
        }

        switch (type) {
            case MolecularFormulaType.Organic:
                particle.flags &= ~ParticleInfo.ParticleFlags.MolecularFormula_1;
                particle.flags &= ~ParticleInfo.ParticleFlags.MolecularFormula_2;
                break;
            case MolecularFormulaType.Acid:
                particle.flags &= ~ParticleInfo.ParticleFlags.MolecularFormula_1;
                particle.flags |= ParticleInfo.ParticleFlags.MolecularFormula_2;
                break;
            case MolecularFormulaType.Normal:
                particle.flags |= ParticleInfo.ParticleFlags.MolecularFormula_1;
                particle.flags &= ~ParticleInfo.ParticleFlags.MolecularFormula_2;
                break;
            case MolecularFormulaType.Reversed:
                particle.flags |= ParticleInfo.ParticleFlags.MolecularFormula_1;
                particle.flags |= ParticleInfo.ParticleFlags.MolecularFormula_2;
                break;
            default:
                particle.flags &= ~ParticleInfo.ParticleFlags.MolecularFormula_1;
                particle.flags &= ~ParticleInfo.ParticleFlags.MolecularFormula_2;
                break;
        }

        // check Carbon valence
        particle.flags &= ~ParticleInfo.ParticleFlags.IncorrectValence;

        bool shouldCheckValence = _uniqueElements.Contains(Element.C);
        if (shouldCheckValence && _uniqueElements.Contains(Element.O) && particle.atoms.Count == 2)
            shouldCheckValence = false; // don't want to remove CO from our DB!

        if (shouldCheckValence) {
            var atoms = particle.atoms;
            var bonds = particle.bonds;
            for (int i = bonds.Count - 1; i >= 0; i--) {
                var b = bonds[i];
                int con = (int)b.bondType + 1;
                if (con > 3) // do not check 4, 0.5, 1.5 bonds
                {
                    _connections[b.atom1] -= 100;
                    _connections[b.atom2] -= 100;
                } else {
                    _connections[b.atom1] += con;
                    _connections[b.atom2] += con;
                }
            }
            for (int i = atoms.Count - 1; i >= 0; i--) {
                if (atoms[i].element == Element.C
                    && _connections[i] > 0 & _connections[i] != 4)
                    particle.flags |= ParticleInfo.ParticleFlags.IncorrectValence;
                _connections[i] = 0;
            }
        }
    }

    private static void UpdateChargeAndRadicalsFlags(ParticleInfo particle)
    {
        bool hasChargedAtoms = false;
        bool hasRadicalAtoms = !string.IsNullOrEmpty(particle.primaryName) && particle.primaryName.Contains("radical");
        var atoms = particle.atoms;

        for (int i = 0; i < atoms.Count; i++) {
            hasChargedAtoms |= atoms[i].atomCharge != 0;
            hasRadicalAtoms |= atoms[i].radical != 0;
        }
        if (particle.charge != 0)
            particle.flags |= ParticleInfo.ParticleFlags.HasParticleCharge;
        else
            particle.flags &= ~ParticleInfo.ParticleFlags.HasParticleCharge;
        if (hasChargedAtoms)
            particle.flags |= ParticleInfo.ParticleFlags.HasAtomCharges;
        else
            particle.flags &= ~ParticleInfo.ParticleFlags.HasAtomCharges;
        if (hasRadicalAtoms)
            particle.flags |= ParticleInfo.ParticleFlags.HasRadicalAtoms;
        else
            particle.flags &= ~ParticleInfo.ParticleFlags.HasRadicalAtoms;
    }
    private static void UpdateSkeletalFormulaFlag(ParticleInfo particle)
    {
        var patoms = particle.atoms;
        var pbonds = particle.bonds;
        int imax = patoms.Count;

        // we will hide C and H in particle only if there are three conected C atoms presented: C-C-C
        bool hideCarbonAndHydrogenInFormula = false;
        int carbons = 0;
        for (int i = patoms.Count - 1; i >= 0; i--) {
            var e = patoms[i].element;
            if (e == Element.C)
                carbons++;
            else if (e != Element.H)
                hideCarbonAndHydrogenInFormula = true;
        }
        hideCarbonAndHydrogenInFormula &= carbons > 0;
        hideCarbonAndHydrogenInFormula |= carbons > 1;

        if (hideCarbonAndHydrogenInFormula)
            particle.flags |= ParticleInfo.ParticleFlags.HasSkeletalFormula;
        else
            particle.flags &= ~ParticleInfo.ParticleFlags.HasSkeletalFormula;
    }
    private static void UpdateHashes(ParticleInfo particle)
    {
        particle.atomsHash = ParticleInfo.GetAtomsHash(particle.atoms);
        particle.structureHash = ParticleComparer.GetStructureHash(particle, false);
        particle.structureHashExact = ParticleComparer.GetStructureHash(particle, true);
    }
    private static void UpdatePrimarilyFlatFlag(ParticleInfo particle)
    {
        var atoms = particle.atoms;
        if (atoms.Count < 4) {
            particle.flags |= ParticleInfo.ParticleFlags.PrimarilyFlat;
            return;
        }
        bool flat = true;
        for (int i = 1; i < atoms.Count; i++) {
            Vector2 p1 = atoms[i].position;
            for (int j = 0; j < i; j++) {
                Vector2 p2 = atoms[j].position;
                if ((p1 - p2).sqrMagnitude < 0.25f) {
                    flat = true;
                    i = int.MaxValue - 1;
                    break;
                }
            }
        }
        if (flat)
            particle.flags |= ParticleInfo.ParticleFlags.PrimarilyFlat;
        else
            particle.flags &= ~ParticleInfo.ParticleFlags.PrimarilyFlat;
    }
    private static HashSet<string> _excludeFromConstructorAndExplorer = null;
    private static void UpdateVisibilityFlags(ParticleInfo particle)
    {
        if (_excludeFromConstructorAndExplorer == null)
            _excludeFromConstructorAndExplorer = ReadLinesFromSourcesFile("ExcludeFromConstructorAndExplorer", true);

        bool include = true;
        if (_excludeFromConstructorAndExplorer.Contains(particle.name)
            || _excludeFromConstructorAndExplorer.Contains(particle.CID.ToString()))
            include = false;
        else {
            include &= (particle.flags & ParticleInfo.ParticleFlags.HasParticleCharge) == 0;    // should NOT have particle charge
            include &= (particle.flags & ParticleInfo.ParticleFlags.HasRadicalAtoms) == 0;      // should NOT have radical atoms
            include &= (particle.flags & ParticleInfo.ParticleFlags.IncorrectValence) == 0;      // should NOT have incorrect valences
            if ((particle.flags & ParticleInfo.ParticleFlags.ForceIncludeInBuild) > 0)
                include = true; // do not look at radicals and charges if it was added manually
            include &= (particle.flags & (ParticleInfo.ParticleFlags.Has2D | ParticleInfo.ParticleFlags.Has3D)) > 0; // should have structure
            include &= (particle.flags & ParticleInfo.ParticleFlags.HasTopology) > 0;           // should have topology
            include &= !string.IsNullOrEmpty(particle.name);                                    // should have name
            if (include)
                include &= particle.maxElement <= MAX_CONSTRUCTOR_ELEMENT;                          // can be assembled in constructor
        }

        if (include)
            particle.flags |= ParticleInfo.ParticleFlags.ShowInConstructor | ParticleInfo.ParticleFlags.ShowInExplorer;
        else
            particle.flags &= ~(ParticleInfo.ParticleFlags.ShowInConstructor | ParticleInfo.ParticleFlags.ShowInExplorer);
    }

    public static XmlDocument ReadXmlFile(string filename)
    {
        // hack that freakin xmlns bullshit.
        string fileData = File.ReadAllText(filename);
        fileData = fileData.Replace(" xmlns=\"", " whocares=\"");
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(fileData);
        return xmlDoc;
    }
    
    private static XmlNode FindChild(XmlNode node, string name)
    {
        var childs = node.ChildNodes;
        for (int i = 0; i < childs.Count; i++) {
            var child = childs[i];
            if (child.Name.Equals(name, StringComparison.Ordinal))
                return child;
        }
        return null;
    }

    private static StringBuilder _sbName = new StringBuilder(50);

    private class PreferredNameAndFormula
    {
        public string name;
        public string formula;
        public int counter;
        public PreferredNameAndFormula(string s)
        {
            var tabIndx = s.IndexOf('\t');
            if (tabIndx > 0) {
                name = ParticleInfo.UnifiedName(s.Substring(0, tabIndx));
                if (tabIndx + 1 < s.Length)
                    formula = s.Substring(tabIndx + 1);
            } else
                name = ParticleInfo.UnifiedName(s);
            counter = 0;
        }
    }
    private static Dictionary<string, PreferredNameAndFormula> _preferredNames = null;
    private static void FindBestName(ParticleInfo particle)
    {
        if (_preferredNames == null) {
            var lines = ReadLinesFromSourcesFile("PreferredNamesAndFormulas", false);
            _preferredNames = new Dictionary<string, PreferredNameAndFormula>(lines.Count);
            foreach (var l in lines) {
                var n = new PreferredNameAndFormula(l);
                _preferredNames.Add(n.name, n);
            }
        }

        particle.flags &= ~ParticleInfo.ParticleFlags.NameIsIUPAC;
        var iupacs = particle.iupacs;

        if (!string.IsNullOrEmpty(particle.primaryName) && particle.primaryName.Contains("radical")) {
            particle.flags |= ParticleInfo.ParticleFlags.HasRadicalAtoms;
            particle.name = particle.primaryName;
            return;
        }

        particle.name = null;

        // in case primary name is preferred - use it
        if (!string.IsNullOrEmpty(particle.primaryName)) {
            PreferredNameAndFormula pnf;
            if (_preferredNames.TryGetValue(particle.primaryName, out pnf)) {
                particle.name = pnf.name;
                if (!string.IsNullOrEmpty(pnf.formula)
                    && !ParticleInfo.GetMolecularFormula(particle, false).Equals(pnf.formula))
                    particle.chemicalFormula = pnf.formula;
                pnf.counter++;
                return;
            }
        }
        // if one of IUPACS is preferred - use it
        if (iupacs != null && iupacs.Count > 0)
            for (int i = 0; i < iupacs.Count; i++) {
                PreferredNameAndFormula pnf;
                if (_preferredNames.TryGetValue(iupacs[i], out pnf)) {
                    particle.name = pnf.name;
                    particle.flags |= ParticleInfo.ParticleFlags.NameIsIUPAC;
                    if (!string.IsNullOrEmpty(pnf.formula)
                        && !ParticleInfo.GetMolecularFormula(particle, false).Equals(pnf.formula))
                        particle.chemicalFormula = pnf.formula;
                    pnf.counter++;
                    return;
                }
            }

        // in case of non-organic compounds - use primary name if name is not SchemblXXXX
        if (!string.IsNullOrEmpty(particle.primaryName)
            && !particle.ContainsBondedAtoms(Element.C, Element.H) && !particle.ContainsBondedAtoms(Element.C, Element.C)) {
            if (!particle.primaryName.StartsWith("Schembl")) {
                particle.name = particle.primaryName;
                return;
            }
        }

        // prefere IUPAC names for organic compounds
        if (iupacs != null && iupacs.Count > 0) {
            // use first IUPAC
            particle.name = iupacs[0];
            particle.flags |= ParticleInfo.ParticleFlags.NameIsIUPAC;
            return;
        }

        // if no IUPAC - use primary one
        if (!string.IsNullOrEmpty(particle.primaryName)) {
            particle.name = particle.primaryName;
            return;
        }

        Debug.LogError("No name for particle " + particle.id + "\n");
    }

    private static HashSet<string> ReadLinesFromSourcesFile(string fileName, bool unify)
    {
        var path = Directory.GetParent(Directory.GetParent(rawMELDB_datapath).FullName).FullName + "/Sources/" + fileName + ".txt";
        var lines = File.ReadAllLines(path);
        var result = new HashSet<string>();
        for (int i = 0; i < lines.Length; i++) {
            var e = lines[i];
            if (e.Length == 0)
                continue;
            if (e[0] == '#')
                continue;
            if (unify)
                result.Add(ParticleInfo.UnifiedName(e));
            else
                result.Add(e);
        }
        return result;
    }

    private static void Clear()
    {
        _excludeFromConstructorAndExplorer = null;
        _molecularOrderExceptions = null;
        _preferredNames = null;
        MelDB.Clear();
    }
}