#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Conservatively cleans the 13 production LessonData assets. The tool changes
/// display text only and verifies that IDs, ordering, slide types, and images
/// remain unchanged before it saves.
/// </summary>
public static class ARDENTLessonTextCleanup
{
    private const int ExpectedLessonCount = 13;
    private const int ExpectedSlideCount = 223;
    private const string HeadingOpen = "<size=115%><b>";
    private const string HeadingClose = "</b></size>";

    private static readonly string[] LessonPaths =
    {
        "Assets/Scripts/COC1_L1.asset",
        "Assets/Scripts/COC1_L2.asset",
        "Assets/Scripts/COC1_L3.asset",
        "Assets/Scripts/COC1_L4.asset",
        "Assets/Scripts/COC2_L1.asset",
        "Assets/Scripts/COC2_L2.asset",
        "Assets/Scripts/COC2_L3.asset",
        "Assets/Scripts/COC2_L4.asset",
        "Assets/Scripts/COC2_L5.asset",
        "Assets/Scripts/COC2_L6.asset",
        "Assets/Scripts/COC2_L7.asset",
        "Assets/Scripts/COC2_L8.asset",
        "Assets/Scripts/COC2_L9.asset"
    };

    private static readonly KeyValuePair<string, string>[] LiteralCorrections =
    {
        Pair("techniquesfor", "techniques for"),
        Pair("icluding", "including"),
        Pair("Sysytem", "System"),
        Pair("FIre", "Fire"),
        Pair("unneccessary", "unnecessary"),
        Pair("unecessary", "unnecessary"),
        Pair("TPower Supply", "Power Supply"),
        Pair("Tools, Equipments", "Tools, Equipment"),
        Pair("Testing Devices (Diagnostic tools)", "Testing Devices (Diagnostic Tools)"),
        Pair("Electro-Static", "Electrostatic"),
        Pair("electro-static", "electrostatic"),
        Pair("Anti Static", "Anti-Static"),
        Pair("anti static", "anti-static"),
        Pair("Screw drivers", "Screwdrivers"),
        Pair("screw drivers", "screwdrivers"),
        Pair("multi-meter", "multimeter"),
        Pair("Multi-meter", "Multimeter"),
        Pair("spays ink", "sprays ink"),
        Pair("Advance Technology Attachment", "Advanced Technology Attachment"),
        Pair("Input Output", "Input/Output"),
        Pair("working with PC’s", "working with PCs"),
        Pair("working with PC's", "working with PCs"),
        Pair("Working with PC’s", "Working with PCs"),
        Pair("Working with PC's", "Working with PCs"),
        Pair("Data Comm’s", "DataComm's"),
        Pair("Data Comm's", "DataComm's"),
        Pair("wall Plates", "wall plates"),
        Pair("CAT5E", "Cat 5e"),
        Pair("Cat5E", "Cat 5e"),
        Pair("Cat5e", "Cat 5e"),
        Pair("RJ 45", "RJ45"),
        Pair("Lan Tester", "LAN tester"),
        Pair("Add- in", "Add-in"),
        Pair("Graphics / Video", "Graphics/Video"),
        Pair("hardcopy", "hard copy"),
        Pair("table top", "tabletop"),
        Pair("Point-out", "Point out"),
        Pair("Explain The PC system", "Explain the PC system"),
        Pair("What is network design", "Explain network design"),
        Pair("What is network", "Explain computer networks"),
        Pair("Plan cable route using designed network", "Plan a cable route using a network design"),
        Pair("Identify materials used for networking", "Identify materials used in networking"),
        Pair("Identify the uses of each material", "Explain the use of each networking material"),
        Pair("How to use the network materials", "Demonstrate how to use networking materials"),
        Pair("Ensuring no unnecessary Damage", "Ensuring No Unnecessary Damage"),
        Pair("Creating a testing Regimen", "Creating a Testing Regimen"),
        Pair("Blind Sports", "Blind Spots"),
        Pair("Network Analysis?", "Network Analysis"),
        Pair("Identify how to ensure that no unnecessary damage occurred while performing installation work and complies with requirements", "Explain how to prevent unnecessary damage while performing installation work in compliance with requirements"),
        Pair("Perform installation work and ensure no unnecessary damage occurred and complies with requirements", "Perform installation work in compliance with requirements while preventing unnecessary damage"),
        Pair("Identify ways of disposing excess components and materials used in networking", "Identify ways to dispose of excess components and materials used in networking"),
        Pair("Follow proper ways of segregation of disposal", "Follow proper methods for segregating waste for disposal"),
        Pair("Follow the 5S Principles in working", "Apply the 5S principles in the workplace"),
        Pair("Don't forget to follow the 5s in working", "Follow the 5S Principles at Work"),
        Pair("Do not forget to follow the 5S in working", "Follow the 5S Principles at Work"),
        Pair("Is a system designed to prevent workplace illnesses and injuries.", "This system is designed to prevent workplace illnesses and injuries."),
        Pair("At work you can use these three Think Safe steps to help prevent\naccidents. Using the Think Safe Steps.", "Use these three Think Safe steps at work to help prevent accidents."),
        Pair("Forms are used to give specific details with regards to the accidents happened in the laboratory during practical activities.", "Accident report forms provide specific details about incidents that occur in the laboratory during practical activities."),
        Pair("may cause short circuit", "may cause a short circuit"),
        Pair("fuses with those proper ratings", "fuses with the proper ratings"),
        Pair("metal fragmented", "metal fragments"),
        Pair("Working area should have ventilations, trash can, fire exit and capable of being disinfect.", "The work area should have proper ventilation, a trash can, a fire exit, and surfaces that can be disinfected."),
        Pair("Port hub /Port", "Port/Hub"),
        Pair("Auotomatic Voltage Regulator", "Automatic Voltage Regulator"),
        Pair("Accelerated Graphic Port", "Accelerated Graphics Port"),
        Pair("Advance Technology Extended", "Advanced Technology Extended"),
        Pair("Basic input Output System", "Basic Input/Output System"),
        Pair("Peripheral Component Interconnector", "Peripheral Component Interconnect"),
        Pair("Power On Self-Test", "Power-On Self-Test"),
        Pair("Personal System 2", "Personal System/2"),
        Pair("mother board", "motherboard"),
        Pair("disk drivers", "disk drives"),
        Pair("different manufactures", "different manufacturers"),
        Pair("from factor", "form factor"),
        Pair("A(Such", "A (such"),
        Pair("B (Such", "B (such"),
        Pair("C(Such", "C (such"),
        Pair("PS/2- The", "<b>PS/2</b> — The"),
        Pair("USB-USB (Universal Serial Bus) is", "<b>USB (Universal Serial Bus)</b> — USB is"),
        Pair("Firewire- Firewire", "<b>FireWire</b> — FireWire"),
        Pair("Look Figure X", "See Figure X"),
        Pair("to the corresponding pin on.", "to the corresponding pins on the motherboard."),
        Pair("How many kinds of Networks?", "Types of Networks"),
        Pair("no. of computers", "number of computers"),
        Pair("10 or less users", "10 or fewer users"),
        Pair("Wide area network", "wide area network"),
        Pair("Networking in any different way needs materials to be used to in order to work.", "Every network installation requires suitable materials to function correctly."),
        Pair("One of the materials are the network cable to be used.", "Network cables are among the essential materials used in an installation."),
        Pair("The following are the example of a guided media network cables:", "Examples of guided network media include:"),
        Pair("8. LAN Card (NIC)- Network Interface Card", "<b>8. LAN Card (NIC)</b> — A network interface card"),
        Pair("Support materials store and protect", "These support structures organize and protect"),
        Pair("switches, routers, server,", "switches, routers, servers,"),
        Pair("Wifi (CARD/USB)", "Wi-Fi (Card/USB)"),
        Pair("In order to set up the networking materials, there are also several tools to be used to accomplish computer networking. manner.", "Several tools are also required to install and maintain a computer network."),
        Pair("Tools to be used in networking", "Tools Used in Networking"),
        Pair("LAN tester- covers the fields of installation and network control.", "<b>LAN tester</b> — Used for network installation and diagnostics."),
        Pair("connected to port and link connectivity", "identify the connected port, and verify link connectivity"),
        Pair("impact action”.", "impact action."),
        Pair("Take away any liquid near your working area", "Remove all liquids from the work area"),
        Pair("Do not use excessive force if things do not quite slip into place.", "Do not use excessive force if components do not fit into place."),
        Pair("Hold on the handle of the crimping tool and not the area were the blades are located.", "Hold the crimping tool by its handles and keep your hands away from the blades."),
        Pair("Contingency measures during workplace accidents, and other emergencies are recognized.", "Follow established contingency procedures during workplace accidents and other emergencies."),
        Pair("Contingency measures during workplace accidents, fire and other emergencies are recognized.", "Follow established contingency procedures during workplace accidents, fires, and other emergencies."),
        Pair("Use brush, compressed air or blower in cleaning the computer system.", "Use a brush, compressed air, or a blower to clean the computer system."),
        Pair("made of few thin wires", "made of a few thin wires"),
        Pair("thus it used for patch cables", "so it is used for patch cables"),
        Pair("Plenum Coated CAT5", "plenum-rated Cat 5"),
        Pair("Ensuring a Specific Level of Cabling Performance UTP", "<size=115%><b>Ensuring a Specific Level of Cabling Performance</b></size>\n\nUTP"),
        Pair("Category 3–,5e–, or 6-compliant", "Category 3, 5e, or 6 compliant"),
        Pair("cross-connect blocks All patch", "cross-connect blocks\n\nAll patch"),
        Pair("requirements for connecting hardware to insure compatibility", "requirements for connecting hardware to ensure compatibility"),
        Pair("Possible questions to ask to ensure that there are no unnecessary damages occurred while work installations are:", "Use the following questions to confirm that no unnecessary damage occurred during installation:"),
        Pair("every one's work", "everyone's work"),
        Pair("consistently- perpetual cleaning", "consistently through ongoing cleaning"),
        Pair("Keep/Store in box or any storage and not in a garbage bin.", "Store unused cable in a box or other storage container rather than a garbage bin."),
        Pair("informations", "information"),
        Pair("equipments", "equipment"),
        Pair("there’s who can", "there is someone who can"),
        Pair("there's who can", "there is someone who can"),
        Pair("so that there’s who can", "so that there is someone who can"),
        Pair("so that there's who can", "so that there is someone who can"),
        Pair("assess to carry out", "access to carry out"),
        Pair("don’t", "do not"),
        Pair("Don’t", "Do not")
    };

    [MenuItem("ARDENT/Lessons/Clean, Format, and Validate All Lesson Text")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before cleaning lesson text.");

        LessonData[] lessons = LoadLessons();
        LessonSnapshot[] before = lessons.Select(Capture).ToArray();
        ValidateDataset(lessons, before);
        string backupFolder = CreateBackup();

        int changedLessons = 0;
        int changedFields = 0;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clean and format ARDENT lesson text");

        try
        {
            for (int i = 0; i < lessons.Length; i++)
            {
                LessonData lesson = lessons[i];
                Undo.RecordObject(lesson, "Clean lesson text");
                int lessonChanges = CleanLesson(lesson);
                if (lessonChanges == 0) continue;
                changedLessons++;
                changedFields += lessonChanges;
                EditorUtility.SetDirty(lesson);
            }

            LessonSnapshot[] after = lessons.Select(Capture).ToArray();
            ValidateDataset(lessons, after);
            ValidateStructureUnchanged(before, after);
            ValidateText(lessons);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Undo.CollapseUndoOperations(undoGroup);
            WriteCompletionReport(backupFolder, lessons, changedLessons, changedFields);
            Debug.Log($"ARDENT lesson cleanup completed: {lessons.Length} lessons, " +
                      $"{lessons.Sum(item => item.slides.Length)} slides, {changedFields} text fields updated. " +
                      $"Backup: {Path.GetFullPath(backupFolder)}");
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("Lesson cleanup stopped before saving. Backup: " + Path.GetFullPath(backupFolder));
            throw;
        }
    }

    // Unity command-line entry point.
    public static void RunBatch()
    {
        try
        {
            Run();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static LessonData[] LoadLessons()
    {
        if (LessonPaths.Length != ExpectedLessonCount)
            throw new InvalidOperationException("The lesson allowlist must contain exactly 13 assets.");

        var lessons = new LessonData[LessonPaths.Length];
        for (int i = 0; i < LessonPaths.Length; i++)
        {
            lessons[i] = AssetDatabase.LoadAssetAtPath<LessonData>(LessonPaths[i]);
            if (lessons[i] == null)
                throw new FileNotFoundException("Could not load LessonData asset.", LessonPaths[i]);
        }
        return lessons;
    }

    private static int CleanLesson(LessonData lesson)
    {
        int changes = 0;
        changes += Replace(ref lesson.lessonTitle, CleanSingleLine(lesson.lessonTitle, false));
        changes += Replace(ref lesson.description, CleanProse(lesson.description));

        if (lesson.learningObjectives != null)
        {
            for (int i = 0; i < lesson.learningObjectives.Length; i++)
            {
                string objective = CleanSingleLine(lesson.learningObjectives[i], true);
                objective = Regex.Replace(objective, @"^[•\-–—→●]\s*", string.Empty);
                objective = UppercaseFirst(objective.Trim());
                if (!Regex.IsMatch(objective, @"[.!?]$")) objective += ".";
                objective = "• " + objective;
                changes += Replace(ref lesson.learningObjectives[i], objective);
            }
        }

        var headings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (lesson.subtopics != null)
        {
            foreach (LessonSubtopic subtopic in lesson.subtopics)
            {
                if (subtopic == null) continue;
                string cleaned = CleanSingleLine(subtopic.subtopicName, false).TrimEnd('.', ':');
                changes += Replace(ref subtopic.subtopicName, cleaned);
                if (!string.IsNullOrWhiteSpace(subtopic.subtopicID))
                    headings[subtopic.subtopicID] = cleaned;
            }
        }

        if (lesson.slides == null) return changes;
        foreach (LessonSlideData slide in lesson.slides)
        {
            if (slide == null) continue;
            headings.TryGetValue(slide.subtopicID ?? string.Empty, out string expectedHeading);
            changes += Replace(ref slide.content, CleanSlide(slide.content, expectedHeading));
        }
        return changes;
    }

    private static string CleanSlide(string value, string expectedHeading)
    {
        string text = Normalize(value);
        if (string.IsNullOrEmpty(text)) return text;

        var lines = text.Split('\n').Select(line => line.Trim()).ToList();
        TrimBlankEdges(lines);

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;

            line = Regex.Replace(line, @"^[•●→*]\s*", "• ");
            line = Regex.Replace(line, @"^[-–—]\s*", "• ");
            line = Regex.Replace(line, @"^(\d+)\.\)\s*", "$1. ");
            line = Regex.Replace(line, @"^(\d+)\)\s*", "$1. ");
            line = Regex.Replace(line, @"^([a-z])\)\s*", "$1. ", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, @"^Step\s+(\d+)\s*[.\-–—:]?\s*(.+)$",
                "<b>Step $1: $2</b>", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, @"^(\d+\.\s+[^:]{2,60}:)\s*$", "<b>$1</b>");
            line = FormatDefinition(line);
            lines[i] = line.Trim();
        }

        int first = lines.FindIndex(line => !string.IsNullOrWhiteSpace(line));
        if (first >= 0 && first + 1 < lines.Count && string.IsNullOrWhiteSpace(lines[first + 1]))
        {
            string plain = StripTags(lines[first]).TrimEnd('.', ':').Trim();
            if (!lines[first].Contains("<b>") && IsHeading(plain, expectedHeading))
                lines[first] = HeadingOpen + plain + HeadingClose;
        }

        text = string.Join("\n", lines);
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = Regex.Replace(text, @"(</size>\n\n)([a-z])", match =>
            match.Groups[1].Value + char.ToUpperInvariant(match.Groups[2].Value[0]));
        text = Regex.Replace(text, @"(<b>15\. LED</b> — )Liquid Crystal Display", "$1Light-Emitting Diode");
        return text.Trim();
    }

    private static string FormatDefinition(string line)
    {
        if (line.StartsWith("<", StringComparison.Ordinal) || line.StartsWith("•", StringComparison.Ordinal))
            return line;

        Match match = Regex.Match(line,
            @"^([A-Za-z0-9][A-Za-z0-9 /&()'’.+\-]{1,52})\s+(?:-|–|—)\s+(?:is\s+|it\s+is\s+)?(.+)$");
        if (!match.Success) return line;
        string term = match.Groups[1].Value.Trim();
        string explanation = UppercaseFirst(match.Groups[2].Value.Trim());
        return "<b>" + term + "</b> — " + explanation;
    }

    private static string CleanProse(string value)
    {
        string text = Normalize(value).Replace("\n", " ");
        text = Regex.Replace(text, @"\s{2,}", " ").Trim();
        return UppercaseFirst(text);
    }

    private static string CleanSingleLine(string value, bool keepBullet)
    {
        string text = Normalize(value).Replace("\n", " ");
        text = Regex.Replace(text, @"\s{2,}", " ").Trim();
        if (!keepBullet) text = Regex.Replace(text, @"^[•\-–—→●]\s*", string.Empty);
        return text;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        string text = value.Replace("\r\n", "\n").Replace('\r', '\n')
            .Replace('\t', ' ').Replace('\u00A0', ' ').Replace('\u202F', ' ');

        foreach (KeyValuePair<string, string> correction in LiteralCorrections)
            text = text.Replace(correction.Key, correction.Value);

        text = Regex.Replace(text, @"\b[Ll][Aa][Nn]\b", "LAN");
        text = Regex.Replace(text, @"\bOHS standard\b", "OHS Standards");
        text = Regex.Replace(text, @"Cabinets and Raceway Racks+\b", "Cabinets and Raceway Racks");
        text = Regex.Replace(text, @"Make sure that the pins are properly aligned when connecting a cable connector(?!\.)",
            "Make sure that the pins are properly aligned when connecting a cable connector.");
        text = Regex.Replace(text, @"\b5s\b", "5S", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bRj45\b", "RJ45", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"[ ]{2,}", " ");
        text = Regex.Replace(text, @" +([,.;:!?])", "$1");
        text = Regex.Replace(text, @"([,.;:!?])(?=[A-Za-z])", "$1 ");
        text = Regex.Replace(text, @"[ ]*\n[ ]*", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    private static bool IsHeading(string line, string expectedHeading)
    {
        if (string.IsNullOrWhiteSpace(line) || line.Length > 90 || line.StartsWith("•")) return false;
        if (Regex.IsMatch(line, @"^\d+[.)]\s")) return false;
        if (Equivalent(line, expectedHeading)) return true;
        if (line.EndsWith("?", StringComparison.Ordinal) || line.EndsWith("!", StringComparison.Ordinal)) return true;
        int words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return words <= 10 && !line.EndsWith(".", StringComparison.Ordinal);
    }

    private static bool Equivalent(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(right)) return false;
        string NormalizeKey(string source) => Regex.Replace(
            StripTags(source ?? string.Empty).ToLowerInvariant(), @"[^a-z0-9]+", string.Empty);
        string a = NormalizeKey(left);
        string b = NormalizeKey(right);
        return a == b || (a.Length > 5 && (a.StartsWith(b) || b.StartsWith(a)));
    }

    private static string StripTags(string value) => Regex.Replace(value ?? string.Empty, "<.*?>", string.Empty);

    private static string UppercaseFirst(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        for (int i = 0; i < value.Length; i++)
        {
            if (!char.IsLetter(value[i])) continue;
            if (char.IsUpper(value[i])) return value;
            char[] characters = value.ToCharArray();
            characters[i] = char.ToUpperInvariant(characters[i]);
            return new string(characters);
        }
        return value;
    }

    private static void TrimBlankEdges(List<string> lines)
    {
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0])) lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1])) lines.RemoveAt(lines.Count - 1);
    }

    private static void ValidateDataset(LessonData[] lessons, LessonSnapshot[] snapshots)
    {
        if (lessons.Length != ExpectedLessonCount)
            throw new InvalidDataException($"Expected {ExpectedLessonCount} lessons; found {lessons.Length}.");
        int slides = lessons.Sum(lesson => lesson.slides?.Length ?? 0);
        if (slides != ExpectedSlideCount)
            throw new InvalidDataException($"Expected {ExpectedSlideCount} slides; found {slides}.");
        if (snapshots.Select(item => item.LessonId).Distinct().Count() != ExpectedLessonCount)
            throw new InvalidDataException("Lesson IDs are missing or duplicated.");
        if (lessons.Any(lesson => lesson.slides == null || lesson.slides.Any(slide => slide == null)))
            throw new InvalidDataException("A lesson contains a null slides array or null slide entry.");
    }

    private static void ValidateStructureUnchanged(LessonSnapshot[] before, LessonSnapshot[] after)
    {
        for (int i = 0; i < before.Length; i++)
        {
            if (!before[i].StructureEquals(after[i]))
                throw new InvalidDataException("Protected lesson structure changed: " + LessonPaths[i]);
        }
    }

    private static void ValidateText(IEnumerable<LessonData> lessons)
    {
        foreach (LessonData lesson in lessons)
        {
            IEnumerable<string> values = new[] { lesson.lessonTitle, lesson.description }
                .Concat(lesson.learningObjectives ?? Array.Empty<string>())
                .Concat(lesson.subtopics?.Where(item => item != null).Select(item => item.subtopicName) ?? Array.Empty<string>())
                .Concat(lesson.slides?.Where(item => item != null).Select(item => item.content) ?? Array.Empty<string>());

            foreach (string value in values)
            {
                if (value == null) throw new InvalidDataException(lesson.name + " contains a null text field.");
                if (value.Contains("\r") || value.Contains("\t"))
                    throw new InvalidDataException(lesson.name + " still contains a carriage return or tab.");
                if (Count(value, "<b>") != Count(value, "</b>") ||
                    Count(value, "<size=") != Count(value, "</size>"))
                    throw new InvalidDataException(lesson.name + " contains unbalanced TextMeshPro tags.");
            }
        }
    }

    private static LessonSnapshot Capture(LessonData lesson) => new LessonSnapshot
    {
        LessonId = lesson.lessonID,
        CocId = lesson.cocID,
        RequiredGoldLessonId = lesson.requiredGoldLessonID,
        SubtopicIds = (lesson.subtopics ?? Array.Empty<LessonSubtopic>())
            .Select(item => item?.subtopicID).ToArray(),
        SlideSubtopicIds = (lesson.slides ?? Array.Empty<LessonSlideData>())
            .Select(item => item?.subtopicID).ToArray(),
        SlideTypes = (lesson.slides ?? Array.Empty<LessonSlideData>())
            .Select(item => item == null ? -1 : (int)item.slideType).ToArray(),
        ImageIds = (lesson.slides ?? Array.Empty<LessonSlideData>())
            .Select(item => item?.image == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item.image)) + ":" + item.image.GetInstanceID())
            .ToArray()
    };

    private static string CreateBackup()
    {
        string folder = Path.Combine(".lesson-backups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(folder);
        var manifest = new StringBuilder();
        manifest.AppendLine("ARDENT LessonData pre-cleanup backup");
        manifest.AppendLine("Created: " + DateTime.Now.ToString("O"));
        foreach (string assetPath in LessonPaths)
        {
            string source = Path.GetFullPath(assetPath);
            string destination = Path.Combine(folder, Path.GetFileName(assetPath));
            File.Copy(source, destination, false);
            if (File.Exists(source + ".meta")) File.Copy(source + ".meta", destination + ".meta", false);
            manifest.AppendLine(Path.GetFileName(assetPath) + "\t" + Sha256(source));
        }
        File.WriteAllText(Path.Combine(folder, "manifest.txt"), manifest.ToString(), new UTF8Encoding(false));
        return folder;
    }

    private static void WriteCompletionReport(string folder, LessonData[] lessons, int changedLessons, int changedFields)
    {
        var report = new StringBuilder();
        report.AppendLine("Validation: PASS");
        report.AppendLine("Lessons: " + lessons.Length);
        report.AppendLine("Slides: " + lessons.Sum(item => item.slides.Length));
        report.AppendLine("Changed lessons: " + changedLessons);
        report.AppendLine("Changed text fields: " + changedFields);
        report.AppendLine("Protected fields: lesson IDs, COC IDs, unlock IDs, subtopic IDs, slide ordering, slide types, images");
        File.WriteAllText(Path.Combine(folder, "validation.txt"), report.ToString(), new UTF8Encoding(false));
    }

    private static string Sha256(string path)
    {
        using (SHA256 hash = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private static int Count(string value, string token)
    {
        int count = 0;
        for (int index = 0; (index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0; index += token.Length)
            count++;
        return count;
    }

    private static int Replace(ref string field, string value)
    {
        if (field == value) return 0;
        field = value;
        return 1;
    }

    private static KeyValuePair<string, string> Pair(string from, string to) =>
        new KeyValuePair<string, string>(from, to);

    [Serializable]
    private sealed class LessonSnapshot
    {
        public int LessonId;
        public string CocId;
        public int RequiredGoldLessonId;
        public string[] SubtopicIds;
        public string[] SlideSubtopicIds;
        public int[] SlideTypes;
        public string[] ImageIds;

        public bool StructureEquals(LessonSnapshot other) =>
            LessonId == other.LessonId && CocId == other.CocId &&
            RequiredGoldLessonId == other.RequiredGoldLessonId &&
            SubtopicIds.SequenceEqual(other.SubtopicIds) &&
            SlideSubtopicIds.SequenceEqual(other.SlideSubtopicIds) &&
            SlideTypes.SequenceEqual(other.SlideTypes) &&
            ImageIds.SequenceEqual(other.ImageIds);
    }
}
#endif
