#region Assembly odaLib, Version=2.1.8060.20806, Culture=neutral, PublicKeyToken=null
// E:\git\OdantDev\Libraries\OdaLibs\odaLib.dll
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System.Drawing;

namespace oda.OdaOverride;
public sealed class INI
{
    public static INI DebugINI
    {
        get
        {
            if (field == null)
            {
                field = new INI("SOLUTION");
                if (Common.Mode == CoreMode.AddIn)
                {
                    field.Clear();
                }
            }

            return field;
        }
    }

    private const string ALL_NODES_XPATH = "*";

    private const string DEFAULT_SETTINGS_DIR = "settings";

    private const string EMPTY_INI_XML = "<INI/>";

    private const string SECTION_ELEMENT = "SECTION";

    private const string VAL_ELEMENT = "VAL";

    private const string VAL_ATTR_NAME = "name";

    private const string VAL_ATTR_COUNT = "count";

    private const string XML_FILE_DOT_EXTENSION = ".xml";

    private static readonly Dictionary<string, WeakReference> iniDocMap = new Dictionary<string, WeakReference>();

    private bool isSaving;

    private bool isChanged;

    private readonly string inClassFileDir = string.Empty;

    private readonly string inClassFileName;

    private readonly SettingsPlace settingsPlace;

    private Class Class { get; set; }

    private xmlDocument? Doc
    {
        get
        {
            if (field == null)
            {
                lock (iniDocMap)
                {
                    field = GetIniDocFromMap(FileName) ?? CreateNewIniDoc(FileName);
                }
            }

            return field;
        }
    }

    private xmlElement? Root => field ??= Doc?.DocumentElement;

    public string? FileName
    {
        get
        {
            if (field == null)
            {
                if (Class != null)
                {
                    if (settingsPlace == SettingsPlace.Local)
                    {
                        var file_name = PathCombine(inClassFileDir, inClassFileName);
                        var fileInfo = loadSettings(Class, file_name, SettingsPlace.Local);
                        if (fileInfo != null && fileInfo.Exists)
                        {
                            field = fileInfo.FullName;
                        }
                    }
                    else
                    {
                        field = Class.FullId;
                    }
                }

                if (field == null)
                {
                    field = PathCombine(UserSettingsPath, "INI", inClassFileDir, inClassFileName);
                }
            }

            return field;
        }
    }

    private string? TmpFileName => field ?? (field = TempFiles.GetTempFileName("xml"));

    public static string? AllSettingsPath
    {
        get
        {
            if (field == null)
            {
                var serverItem = ItemFactory.Connection.ServerItem;
                if (serverItem != null)
                {
                    field = Utils.forceDirectory(serverItem.SharedDir);
                }
            }

            return field;
        }
    }

    public static string UserSettingsPath
    {
        get
        {
            if (string.IsNullOrEmpty(field))
            {
                var dirPath = ((ItemFactory.Connection.ServerItem == null) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ODA") : ItemFactory.Connection.ServerItem.UserAppDir);
                field = Utils.forceDirectory(dirPath);
            }

            return field;
        }
    }

    public string[] Sections
    {
        get
        {
            if (!(Root != null))
            {
                return null;
            }

            return Root.XQuery("string-join(//SECTION/@name,'|')").Split('|');
        }
    }

    public string XML
    {
        get
        {
            if (!(Root != null))
            {
                return string.Empty;
            }

            return Root.XML;
        }
    }

    public int PartCount
    {
        get
        {
            if (!(Root != null))
            {
                return 0;
            }

            return Root.ChildNodes.Count;
        }
    }

    public INI(Class cls, string name, SettingsPlace place)
    {
        Class = cls;
        settingsPlace = place;
        inClassFileName = Utils.CorrectFileName(name);
        if (!inClassFileName.EndsWith(".xml"))
        {
            inClassFileName += ".xml";
        }
    }

    public INI(Class cls, string name)
    {
        Class = cls;
        inClassFileName = Utils.CorrectFileName(name);
        if (!inClassFileName.EndsWith(".xml"))
        {
            inClassFileName += ".xml";
        }
    }

    public INI(Class cls, object sender)
        : this(cls, sender.ToString())
    {
    }

    public INI(object sender)
        : this(sender.GetType().ToString(), sender.ToString())
    {
    }

    public INI(string name)
        : this((Class)null, name)
    {
    }

    public INI(string part, string name)
        : this((Class)null, name)
    {
        inClassFileDir = part;
    }

    internal void Write(string section, object key, object value, int index)
    {
        if (Root == null)
        {
            return;
        }

        var text = Utils.ObjectToString(key);
        var xmlElement = (string.IsNullOrEmpty(section) ? Root : SelectOrCreateElementByName(Root, "SECTION", section));
        if (xmlElement != null)
        {
            var xmlElement2 = SelectOrCreateElementByName(xmlElement, "VAL", text);
            if (xmlElement2 == null)
            {
                throw new Exception("Can't create element '" + text + " in section '" + section + "'");
            }

            var name = "v" + index;
            var value2 = Utils.ObjectToString(value);
            xmlElement2.SetAttribute(name, value2);
        }
        isChanged = true;
    }

    public void Write(string section, object key, object value)
    {
        var text = Utils.ObjectToString(key);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Remove(section, text);
        if (value is object[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                Write(section, key, array[i], i);
            }
        }
        else if (value is IList<object> list)
        {
            for (var j = 0; j < list.Count; j++)
            {
                Write(section, key, list[j], j);
            }
        }
        else
        {
            Write(section, key, value, 0);
        }
    }

    public void Set(object key, object value)
    {
        Write(string.Empty, key, value);
    }

    public string ReadString(string section, object key, object def_value)
    {
        var text = ReadArrayValue(section, key, 0);
        if (text.Length == 0)
        {
            text = Utils.ObjectToString(def_value);
        }

        return text;
    }

    public string ReadString(object key, object def_value)
    {
        return ReadString(null, key, def_value);
    }

    public string ReadString(object key)
    {
        return ReadString(null, key, string.Empty);
    }

    public string[] ReadArray(string section, object key, object def_value)
    {
        if (Root == null)
        {
            return (string[])def_value;
        }

        var name = Utils.ObjectToString(key);
        var xPath = GetXPath(section, name);
        var xmlElement = Root.SelectElement(xPath);
        if (xmlElement == null)
        {
            if (def_value is string[])
            {
                return (string[])def_value;
            }

            return Array.Empty<string>();
        }

        return xmlElement.XQuery("string-join(@*[starts-with(name(),'v')], '|')").Split('|');
    }

    public string ReadArrayValue(string section, object key, int index)
    {
        var array = ReadArray(section, key, index);
        if (array.Length > index)
        {
            return array[index];
        }

        return string.Empty;
    }

    public string ReadArrayValue(object key, int index)
    {
        return ReadArrayValue(null, key, index);
    }

    public bool ReadBool(string section, object key, bool default_value)
    {
        if (!bool.TryParse(ReadString(section, key, default_value), out var result))
        {
            return default_value;
        }

        return result;
    }

    public bool ReadBool(string section, object key)
    {
        return ReadBool(section, key, default_value: false);
    }

    public bool ReadBool(object key)
    {
        return ReadBool(null, key, default_value: false);
    }

    public DateTime ReadDateTime(string section, object key, DateTime default_value)
    {
        if (!DateTime.TryParse(ReadString(section, key, default_value), out var result))
        {
            return default_value;
        }

        return result;
    }

    public DateTime ReadDateTime(string section, object key)
    {
        return ReadDateTime(section, key, DateTime.MinValue);
    }

    public DateTime ReadDateTime(object key)
    {
        return ReadDateTime(null, key, DateTime.MinValue);
    }

    public int ReadInt(string section, object key, int default_value)
    {
        if (!int.TryParse(ReadString(section, key, default_value), out var result))
        {
            return default_value;
        }

        return result;
    }

    public int ReadInt(string section, object key)
    {
        return ReadInt(section, key, 0);
    }

    public int ReadInt(object key)
    {
        return ReadInt(null, key, 0);
    }

    public Rectangle ReadRect(string section, object key, Rectangle default_value)
    {
        var rectangleConverter = new RectangleConverter();
        var text = ReadString(section, key, rectangleConverter.ConvertToString(default_value));
        if (text.Length == 0)
        {
            return default_value;
        }

        try
        {
            if (rectangleConverter.ConvertToString(default_value) != text)
            {
                var obj = rectangleConverter.ConvertFromString(text);
                if (obj != null)
                {
                    return (Rectangle)obj;
                }

                return default_value;
            }

            return default_value;
        }
        catch
        {
            return default_value;
        }
    }

    public Rectangle GetRect(string section, object key)
    {
        return ReadRect(section, key, default(Rectangle));
    }

    public Rectangle GetRect(object key)
    {
        return GetRect(null, key);
    }

    public void Clear()
    {
        if (Root != null)
        {
            Root.RemoveNodes("*");
        }
    }

    public async Task<bool> SaveAsync()
    {
        return await Task.Run(Save);
    }
    public bool Save()
    {
        if (!isChanged || isSaving)
        {
            return true;
        }

        var root = Root;
        if (root == null)
        {
            return false;
        }

        isSaving = true;
        try
        {
            root.RemoveNodes("//VAL[not(@name)]");
            root.RemoveNodes("SECTION[not(*)]");
            if (Class != null)
            {
                Doc.SaveBinary(TmpFileName);
                saveSettings(Class, TmpFileName, inClassFileName, settingsPlace);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FileName));
                Doc.SaveBinary(FileName);
            }
            isChanged = false;
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            isSaving = false;
        }
    }

    public string[] ReadSection(string section)
    {
        if (Root == null)
        {
            return Array.Empty<string>();
        }

        var xquery = (string.IsNullOrEmpty(section) ? "string-join(VAL/@name,'|')" : ("string-join(//SECTION[@name = '" + section + "']/VAL/@name,'|')"));
        var text = Root.XQuery(xquery);
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        return text.Split('|');
    }

    public string[] ReadSection()
    {
        return ReadSection(null);
    }

    public void Remove(string sect, string name)
    {
        if (!(Root == null))
        {
            Root.RemoveNodes("SECTION[@name='" + sect + "']/VAL[@name='" + name + "']");
        }
    }

    public void ClearSection()
    {
        if (!(Root == null))
        {
            Root.RemoveNodes("VAL");
        }
    }

    public void ClearSection(string name)
    {
        if (!(Root == null))
        {
            Root.RemoveNodes("SECTION[@name='" + name + "']");
        }
    }

    public void RemoveValue(object key)
    {
        RemoveValue(null, key);
    }

    public void RemoveValue(string section, object key)
    {
        if (!(Root == null))
        {
            var text = Utils.ObjectToString(key);
            if (!string.IsNullOrEmpty(text))
            {
                Root.RemoveNodes(GetXPath(section, text));
            }
        }
    }

    private static string GetXPath(string section, string name)
    {
        if (!string.IsNullOrEmpty(section))
        {
            return "SECTION[@name='" + section + "']/VAL[@name='" + name + "']";
        }

        return "VAL[@name='" + name + "']";
    }

    public void AddRecent(string section, string name)
    {
        if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(name) || Root == null)
        {
            return;
        }

        var xmlElement = SelectOrCreateElementByName(Root, "SECTION", section);
        if (!(xmlElement == null))
        {
            var xmlElement2 = SelectOrCreateElementByName(xmlElement, "VAL", name);
            if (!(xmlElement2 == null))
            {
                xmlElement2.SetAttribute("date", DateTime.Now);
                var @int = xmlElement2.GetInt("count");
                xmlElement2.SetAttribute("count", ++@int);
                xmlElement.RemoveNodes("subsequence(for $a in VAL order by string-join($a/(@date, @count), '-') descending return $a, 21)");
            }
        }
    }

    public string[] GetRecent(string section)
    {
        if (string.IsNullOrEmpty(section) || Root == null)
        {
            return Array.Empty<string>();
        }

        var xquery = "string-join(subsequence(for $a in SECTION[@name='" + section + "']/VAL order by $a/(@date) descending return $a/@name, 1, 20), '|')";
        return Root.XQuery(xquery).Split('|');
    }

    public static FileInfo loadSettings(Class cls, string file_name, SettingsPlace place)
    {
        FileInfo fileInfo = null;
        switch (place)
        {
            case SettingsPlace.Remote:
                {
                    var settingsFile = getSettingsFile(cls, file_name);
                    if (settingsFile != null)
                    {
                        fileInfo = new FileInfo(settingsFile.Load());
                    }

                    break;
                }
            case SettingsPlace.Local:
                if (!file_name.Contains(":\\"))
                {
                    var classOrganisation = GetClassOrganisation(cls);
                    if (classOrganisation != null)
                    {
                        file_name = PathCombine(UserSettingsPath, "settings", classOrganisation.Id, cls.Id, file_name);
                        if (!string.IsNullOrEmpty(file_name))
                        {
                            fileInfo = new FileInfo(file_name);
                            if (fileInfo.Exists)
                            {
                                return fileInfo;
                            }
                        }
                    }

                    file_name = PathCombine(UserSettingsPath, "settings", cls.Id, file_name);
                }

                fileInfo = new FileInfo(file_name);
                fileInfo.Refresh();
                if (!fileInfo.Exists)
                {
                    if (cls.Parent != null)
                    {
                        fileInfo = loadSettings(cls.Parent as Class, file_name, place);
                    }

                    if ((fileInfo == null || !fileInfo.Exists) && cls.Class != null)
                    {
                        fileInfo = loadSettings(cls.Class, file_name, place);
                    }
                }

                break;
        }

        return fileInfo;
    }

    private static File getSettingsFile(Class cls, string file_name)
    {
        if (!file_name.StartsWith("settings\\"))
        {
            file_name = Path.Combine("settings", file_name);
        }

        var file = cls.Dir.GetFile(file_name);
        if (file == null)
        {
            if (cls.Parent != null)
            {
                file = getSettingsFile(cls.Parent as Class, file_name);
            }

            if (file == null && cls.Class != null)
            {
                file = getSettingsFile(cls.Class, file_name);
            }

            if (file == null && cls.TypeClass != null)
            {
                file = getSettingsFile(cls.TypeClass, file_name);
            }
        }

        return file;
    }

    public static void saveSettings(Class cls, string from_file, string to_file, SettingsPlace place)
    {
        if (string.IsNullOrEmpty(from_file) || string.IsNullOrEmpty(to_file) || cls == null || string.IsNullOrEmpty(Path.GetFileName(to_file)))
        {
            return;
        }

        try
        {
            switch (place)
            {
                case SettingsPlace.Remote:
                    to_file = Path.Combine("settings", to_file);
                    cls.SaveFile(from_file, to_file);
                    break;
                case SettingsPlace.Local:
                    {
                        var classOrganisation = GetClassOrganisation(cls);
                        var path = ((classOrganisation == null) ? PathCombine(UserSettingsPath, "settings", cls.Id) : PathCombine(UserSettingsPath, "settings", classOrganisation.Id, cls.Id));
                        to_file = Path.Combine(path, to_file);
                        Directory.CreateDirectory(Path.GetDirectoryName(to_file));
                        System.IO.File.Copy(from_file, to_file, overwrite: true);
                        break;
                    }
            }
        }
        catch
        {
        }
    }

    public static void removeSettings(Class cls, string file)
    {
        var dir = cls.Dir;
        var num = file.IndexOf(Path.DirectorySeparatorChar);
        while (num > 0 && dir != null)
        {
            var path = file.Substring(0, num);
            dir = dir.GetDir(path);
            file = file.Substring(num + 1);
            num = file.IndexOf(Path.DirectorySeparatorChar);
        }

        if (dir != null && !string.IsNullOrEmpty(file))
        {
            dir.DeleteFile(file);
        }
    }

    public static string getUserSettingsPath(string path)
    {
        return PathCombine(UserSettingsPath, path);
    }

    public static string GetAllSettingsPath(string path)
    {
        return PathCombine(AllSettingsPath, path);
    }

    private static string PathCombine(string path1, params string[] path2)
    {
        if (path1 == null)
        {
            path1 = string.Empty;
        }

        if (path2 != null)
        {
            foreach (var text in path2)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    path1 = Path.Combine(path1, text);
                }
            }
        }

        return path1;
    }

    private static xmlDocument GetIniDocFromMap(string fileName)
    {
        if (iniDocMap.TryGetValue(fileName, out var value))
        {
            if (value != null)
            {
                var xmlDocument = value.Target as xmlDocument;
                if (xmlDocument != null && !xmlDocument.IsNull)
                {
                    return xmlDocument;
                }
            }

            iniDocMap.Remove(fileName);
        }

        return null;
    }

    private xmlDocument CreateNewIniDoc(string fileName)
    {
        var xmlDocument = new xmlDocument();
        if (settingsPlace == SettingsPlace.Remote)
        {
            var fileInfo = loadSettings(Class, inClassFileName, SettingsPlace.Remote);
            if (fileInfo != null)
            {
                xmlDocument.Load(fileInfo.FullName);
            }
        }
        else if (!string.IsNullOrEmpty(Path.GetFullPath(fileName)) && System.IO.File.Exists(fileName))
        {
            xmlDocument.Load(fileName);
        }

        if (xmlDocument.Root == null)
        {
            xmlDocument.LoadXML("<INI/>");
        }

        var value = new WeakReference(xmlDocument, trackResurrection: false);
        iniDocMap.Remove(fileName);
        iniDocMap.Add(fileName, value);
        return xmlDocument;
    }

    private static xmlElement SelectOrCreateElementByName(xmlElement root, string name, string attrName)
    {
        var xpath = name + "[@name='" + attrName + "']";
        var xmlElement = root.SelectElement(xpath);
        if (xmlElement == null)
        {
            xmlElement = root.CreateChildElement(name);
            if (xmlElement != null)
            {
                xmlElement.SetAttribute("name", attrName);
            }
        }

        return xmlElement;
    }

    internal static Domain GetClassOrganisation(Class cls)
    {
        if (cls == null)
        {
            return null;
        }

        Domain domain;
        for (domain = cls.Domain; domain != null; domain = domain.Owner as Domain)
        {
            var text = domain.Type.ToLower();
            if (text.Equals("part") || text.Equals("organization") || text.Equals("base"))
            {
                break;
            }
        }

        return domain;
    }
}
