using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Xml;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ProjectRimFactory_ModSettings : ModSettings
    {
        public static bool allowAllMultipleSpecialSculptures;
        public static Dictionary<string, int> maxNumbersSpecialSculptures;

        public static void LoadXml(ModContentPack content)
        {
            root = ParseSettingRows(content);
            root.Initialize();
        }

        public static DefChangeTracker defTracker = new DefChangeTracker();

        private static ContainerRow root;

        private static void AddHeader(Listing_Standard list, string header)
        {
            var rect = list.GetRect(30);

            Widgets.DrawRectFast(rect, Color.gray);
            var tmp = Text.Font;
            var tmpAnc = Text.Anchor;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(rect, header);
            Text.Font = tmp;
            Text.Anchor = tmpAnc;

            list.Gap();
        }

        // All C# based mod settings can go here.  If better organization
        //   is desired, we can set up some ContainerRow classes that are
        //   organized by XML?  But that's a lot of work.
        private static void CSharpSettings(Listing_Standard list)
        {
            // Style: do your section of settings and then list.GapLine();
            AddHeader(list, "PRF_Settings_C_Lite_Header".Translate());

            var rect = list.GetRect(20);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            TooltipHandler.TipRegion(rect, "PRF_Settings_C_Lite_ToolTip".Translate());
            Widgets.CheckboxLabeled(rect, "PRF_Settings_C_Lite_Label".Translate(), ref PRF_LiteMode);
            list.Gap();

            if (PRF_LiteMode != PRF_LiteMode_last)
            {
                PRF_CustomizeDefs.ToggleLiteMode(PRF_LiteMode);
            }

            PRF_LiteMode_last = PRF_LiteMode;

            AddHeader(list, "PRF_Settings_C_Patches_Header".Translate());

            rect = list.GetRect(20);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            TooltipHandler.TipRegion(rect, "PRF_Settings_C_Patches_Reachability_CanReach_ToolTip".Translate());
            Widgets.CheckboxLabeled(rect, "PRF_Settings_C_Patches_Reachability_CanReach".Translate(), ref PRF_Patch_Reachability_CanReach);
            list.Gap();
            ConditionalPatchHelper.Patch_Reachability_CanReach.PatchHandler(PRF_Patch_Reachability_CanReach);
        }

        private static ContainerRow ParseSettingRows(ModContentPack content)
        {
            var r = new ContainerRow();
            var xmlDoc = DirectXmlLoader.XmlAssetsInModFolder(content, "Settings")?.Where(x => x.name == "Settings.xml")?.ToList().FirstOrDefault();
            if (xmlDoc == null || xmlDoc.xmlDoc == null)
            {
                Log.Error("Settings/Settings.xml not found or invalid xml.");
                return r;
            }

            var rootElem = xmlDoc.xmlDoc.DocumentElement;
            if (rootElem.Name != "SettingRows")
            {
                Log.Error("SettingRows not found. name=" + rootElem.Name);
                return r;
            }

            r.Rows.LoadDataFromXmlCustom(rootElem);
            return r;
        }

        public static bool PRF_LiteMode = false;
        private static bool PRF_LiteMode_last = false;
        public static bool PRF_Patch_Reachability_CanReach = false;
        public static bool PRF_Patch_WealthWatcher_CalculateWealthItems = true;

        public override void ExposeData()
        {
            base.ExposeData();
            root.ExposeData();
            Scribe_Values.Look<Debug.Flag>(ref Debug.activeFlags, "debugFlags", 0);
            Scribe_Values.Look(ref PRF_LiteMode, "PRF_LiteMode", false);
            Scribe_Values.Look(ref PRF_Patch_Reachability_CanReach, "PRF_Patch_Reachability_CanReach", false);
            Scribe_Values.Look(ref PRF_Patch_WealthWatcher_CalculateWealthItems, "PRF_Patch_WealthWatcher_CalculateWealthItems", true);
            PRF_LiteMode_last = PRF_LiteMode;
        }

        public void DoWindowContents(Rect inRect)
        {
            var outRect = new Rect(inRect);
            outRect.yMin += 20f;
            outRect.yMax -= 20f;
            outRect.xMin += 20f;
            outRect.xMax -= 20f;

            var viewRect = new Rect(0f, 0f, outRect.width - 16f, lastHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var list = new Listing_Standard();
            list.Begin(viewRect);
#if DEBUG
            list.Label("Debug Symbols:");
            foreach (var f in (Debug.Flag[])Enum.GetValues(typeof(Debug.Flag)))
            {
                var ischecked = (f & Debug.activeFlags) > 0;
                list.CheckboxLabeled(f.ToString(), ref ischecked, f.ToString()); // use Desc to force list to highlight
                if (!ischecked == (f & Debug.activeFlags) > 0)
                {
                    Debug.activeFlags ^= f; // toggle f
                }
            }

            list.GapLine();
#endif
            CSharpSettings(list);
            root.Draw(list);
            list.End();
            Widgets.EndScrollView();

            lastHeight = list.CurHeight + 16f;
        }

        private Vector2 scrollPosition;
        private float lastHeight = 1000f;

        public void Apply()
        {
            root.Apply();
        }

        public bool RequireReboot => root.RequireReboot;

        public virtual IEnumerable<PatchOperation> Patches => root.GetValidPatches();
    }

    public interface ISettingRow
    {
        void Draw(Listing_Standard list);
        void ExposeData();
        void Apply();
        bool RequireReboot { get; }
        bool Initialize();
        IEnumerable<PatchOperation> GetValidPatches();
    }

    public abstract class SettingRow : ISettingRow
    {
        public bool RequireReboot => false;

        public void Apply()
        {
        }

        public abstract void Draw(Listing_Standard list);

        public void ExposeData()
        {
        }

        public IEnumerable<PatchOperation> GetValidPatches()
        {
            return Enumerable.Empty<PatchOperation>();
        }

        public bool Initialize()
        {
            return true;
        }
    }

    public abstract class SettingItemBase : ISettingRow
    {
        public string key;
        public string label;
        public string description;
        public virtual bool RequireReboot { get; protected set; } = false;

        public abstract void Draw(Listing_Standard list);

        public abstract void ExposeData();

        public abstract void Apply();

        public virtual bool Initialize()
        {
            return true;
        }

        public abstract IEnumerable<PatchOperation> GetValidPatches();
    }

    public abstract class SettingItem : SettingItemBase
    {
        public override IEnumerable<PatchOperation> GetValidPatches()
        {
            return Enumerable.Empty<PatchOperation>();
        }
    }

    public abstract class PatchSettingItem : SettingItemBase
    {
        public PatchElement Patch;

        protected IEnumerable<PatchOperation> Patches => Patch?.Patches ?? Enumerable.Empty<PatchOperation>();
    }

    public class PatchElement
    {
        public List<PatchOperation> Patches { get; private set; }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            rootNode = xmlRoot;
            Patches = LoadDataFromXml(xmlRoot);
        }

        public static List<PatchOperation> LoadDataFromXml(XmlNode xmlRoot)
        {
            return xmlRoot.ChildNodes.Cast<XmlNode>()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Select(n => n as XmlElement)
                .Where(e => e != null)
                .Where(e => e.Name == "Operation")
                .Select(e => DirectXmlToObject.ObjectFromXml<PatchOperation>(e, false))
                .ToList();
        }

        public XmlNode rootNode;
    }

    public abstract class ContainerRowBase : ISettingRow
    {
        protected List<ISettingRow> rows = new List<ISettingRow>();

        public bool RequireReboot => rows.Any(r => r.RequireReboot);

        public void Apply()
        {
            rows.ForEach(r => r.Apply());
        }

        public abstract void Draw(Listing_Standard list);

        public void ExposeData()
        {
            rows.ForEach(r => r.ExposeData());
        }

        public IEnumerable<PatchOperation> GetValidPatches()
        {
            return rows.SelectMany(r => r.GetValidPatches());
        }

        public abstract bool Initialize();
    }

    public class RowsElement
    {
        public List<ISettingRow> rows;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            rows = LoadDataFromXml(xmlRoot);
        }

        public static List<ISettingRow> LoadDataFromXml(XmlNode xmlRoot)
        {
            return xmlRoot.ChildNodes.Cast<XmlNode>()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Select(n => n as XmlElement)
                .Where(e => e != null)
                .Where(e => e.Name == "Row")
                .Select(e => DirectXmlToObject.ObjectFromXml<ISettingRow>(e, false))
                .ToList();
        }
    }

    public class ContainerRow : ContainerRowBase
    {
        public RowsElement Rows = new RowsElement();

        public Color backgroundColor = Color.clear;

        private float lastHeight = 100000;

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(lastHeight);
            if (backgroundColor != Color.clear)
            {
                Widgets.DrawRectFast(rect, backgroundColor);
            }

            var child = new Listing_Standard();
            child.Begin(rect);
            rows.ForEach(r => r.Draw(child));
            child.End();
            lastHeight = child.CurHeight;
            list.Gap(list.verticalSpacing);
        }

        public override bool Initialize()
        {
            if (Rows == null || Rows.rows == null)
            {
                return false;
            }

            rows = Rows.rows.Where(r => r.Initialize()).ToList();
            return rows.Count > 0;
        }
    }

    public class SplitRow : ContainerRowBase
    {
        public float rate = 0.5f;

        public ISettingRow LeftRow;

        public ISettingRow RightRow;

        private float lastHeight = 300;

        public Color leftBackgroundColor = Color.clear;

        public Color rightBackgroundColor = Color.clear;

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(lastHeight);

            var lr = new[]
            {
                new { Row = LeftRow, List = new Listing_Standard(), Rect = rect.LeftPart(rate), BGColor = leftBackgroundColor },
                new { Row = RightRow, List = new Listing_Standard(), Rect = rect.RightPart(1f - rate), BGColor = rightBackgroundColor }
            }.ToList();

            lr.ForEach(
                s =>
                {
                    if (s.BGColor != Color.clear)
                    {
                        Widgets.DrawRectFast(s.Rect, s.BGColor);
                    }

                    s.List.Begin(s.Rect);
                    s.Row.Draw(s.List);
                    s.List.End();
                }
            );
            lastHeight = lr.Select(s => s.List.CurHeight).Max();

            list.Gap(list.verticalSpacing);
        }

        public override bool Initialize()
        {
            rows = new ISettingRow[] { LeftRow, RightRow }.Where(r => r.Initialize()).ToList();
            return rows.Count > 0;
        }
    }

    public class TextRow : SettingRow
    {
        public GameFont font = GameFont.Small;
        public TextAnchor anchor = TextAnchor.MiddleLeft;
        public string text = "";
        public float height;
        public Color backgroundColor = Color.clear;
        public bool noTranslate = false;

        public override void Draw(Listing_Standard list)
        {
            var tmp = Text.Font;
            var tmpAnc = Text.Anchor;
            try
            {
                Text.Font = font;
                Text.Anchor = anchor;
                var h = height;
                var t = text.Translate();
                if (h == 0)
                {
                    h = Text.CalcHeight(t, list.ColumnWidth);
                }

                var rect = list.GetRect(h);
                if (backgroundColor != Color.clear)
                {
                    Widgets.DrawRectFast(rect, backgroundColor);
                }

                var label = text.Translate();
                if (noTranslate)
                {
                    label = text;
                }

                Widgets.Label(rect, label);
                list.Gap(list.verticalSpacing);
            }
            finally
            {
                Text.Font = tmp;
                Text.Anchor = tmpAnc;
            }
        }
    }

    public class ImageRow : SettingRow
    {
        public string texPath;
        public float height;
        public Color backgroundColor = Color.clear;

        public override void Draw(Listing_Standard list)
        {
            var tex = ContentFinder<Texture2D>.Get(texPath, true);
            var h = height;
            if (h == 0)
            {
                h = tex.height;
            }

            var rect = list.GetRect(h);
            if (backgroundColor != Color.clear)
            {
                Widgets.DrawRectFast(rect, backgroundColor);
            }

            Widgets.DrawTextureFitted(rect, tex, 1);
            list.Gap(list.verticalSpacing);
        }
    }

    public class GapLineRow : SettingRow
    {
        public float height = 12f;
        public Color color = Color.clear;

        public override void Draw(Listing_Standard list)
        {
            var tmp = GUI.color;
            try
            {
                if (color != Color.clear)
                {
                    GUI.color = color;
                }

                if (height != 0f)
                {
                    list.GapLine(height);
                }
            }
            finally
            {
                GUI.color = tmp;
            }
        }
    }

    public class GapRow : SettingRow
    {
        public float height = 12f;

        public override void Draw(Listing_Standard list)
        {
            if (height != 0f)
            {
                list.Gap(height);
            }
        }
    }

    public class PatchItem : PatchSettingItem
    {
        private bool checkOn = false;
        private bool currentCheckOn;

        public override IEnumerable<PatchOperation> GetValidPatches()
        {
            return checkOn ? Patches : Enumerable.Empty<PatchOperation>();
        }

        public override void Apply()
        {
            if (currentCheckOn != checkOn)
            {
                RequireReboot = true;
                checkOn = currentCheckOn;
            }
        }

        public override void Draw(Listing_Standard list)
        {
            list.CheckboxLabeled(label.Translate(), ref currentCheckOn, description.Translate());
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref checkOn, key);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                currentCheckOn = checkOn;
            }
        }
    }

    public abstract class PatchValueItem : PatchSettingItem
    {
        public bool checkOn = false;
        protected bool currentCheckOn;

        protected abstract string ReplaceText { get; }

        protected List<PatchOperation> replaced;

        protected IEnumerable<PatchOperation> ReplacedPatch
        {
            get
            {
                if (replaced == null)
                {
                    var xmlText = Patch.rootNode.OuterXml.Replace("${value}", SecurityElement.Escape(ReplaceText));
                    var doc = new XmlDocument();
                    doc.LoadXml(xmlText);
                    var p = PatchElement.LoadDataFromXml(doc.FirstChild);
                    replaced = PatchElement.LoadDataFromXml(doc.FirstChild);
                }

                return replaced;
            }
        }

        public override void Apply()
        {
            if (checkOn != currentCheckOn)
            {
                RequireReboot = true;
                checkOn = currentCheckOn;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref checkOn, key + "__check");
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                currentCheckOn = checkOn;
            }
        }

        public override IEnumerable<PatchOperation> GetValidPatches()
        {
            return checkOn ? ReplacedPatch : Enumerable.Empty<PatchOperation>();
        }
    }

    public abstract class PatchValueItem<T> : PatchValueItem
    {
        public T value;

        protected T currentValue;

        public override void Apply()
        {
            base.Apply();
            if (!Equals(value, currentValue))
            {
                RequireReboot = true;
                value = currentValue;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<T>(ref value, key);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                currentValue = value;
            }
        }

        protected override string ReplaceText => value.ToString();

        public override bool Initialize()
        {
            currentValue = value;
            return base.Initialize();
        }
    }

    public class PatchTextValueItem : PatchValueItem<string>
    {
        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);
            currentValue = Widgets.TextField(rect.RightHalf(), currentValue);
            list.Gap(list.verticalSpacing);
        }
    }

    public class PatchFloatValueItem : PatchValueItem<float>
    {
        public float minValue = 0f;

        public float maxValue = 100000f;

        public float roundTo = -1;

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight * 2f);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);

            var rectSlider = rect.RightHalf();
            rectSlider.xMin += 20;
            rectSlider.xMax -= 20;
            currentValue = Widgets.HorizontalSlider_NewTemp(
                rectSlider,
                currentValue,
                minValue,
                maxValue,
                true,
                currentValue.ToString(),
                minValue.ToString(),
                maxValue.ToString(),
                roundTo
            );
            list.Gap(list.verticalSpacing);
        }
    }

    public class PatchIntValueItem : PatchValueItem<int>
    {
        public int minValue = 0;

        public int maxValue = 100000;

        public int roundTo = 1;

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight * 2f);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);

            var rectSlider = rect.RightHalf();
            rectSlider.xMin += 20;
            rectSlider.xMax -= 20;
            currentValue = (int)Widgets.HorizontalSlider_NewTemp(
                rectSlider,
                currentValue,
                minValue,
                maxValue,
                true,
                currentValue.ToString(),
                minValue.ToString(),
                maxValue.ToString(),
                roundTo
            );
            list.Gap(list.verticalSpacing);
        }
    }

    public class PatchBoolValueItem : PatchValueItem<bool>
    {
        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);

            Widgets.Checkbox(rect.RightHalf().position, ref currentValue);
            list.Gap(list.verticalSpacing);
        }
    }

    public class PatchEnumValueItem : PatchValueItem<int>
    {
        public Type enumType;

        public List<object> EnumValues => enumType.GetEnumValues().Cast<object>().ToList();

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);
            if (Widgets.ButtonText(rect.RightHalf(), "PRF.Settings.Select".Translate() + " (" + EnumValues[currentValue] + ")"))
            {
                Find.WindowStack.Add(
                    new FloatMenu(enumType.GetEnumValues().Cast<object>().Select((o, idx) => new FloatMenuOption(o.ToString(), () => currentValue = idx)).ToList())
                );
            }

            list.Gap(list.verticalSpacing);
        }

        protected override string ReplaceText => EnumValues[value].ToString();

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;
            if (enumType == null || enumType.GetEnumValues().Cast<object>().Count() == 0)
            {
                Log.Error("invalid enumType on Settings.xml");
                return false;
            }

            return true;
        }
    }

    public class PatchSelectValueItem : PatchValueItem<int>
    {
        public List<string> options;

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);
            if (Widgets.ButtonText(rect.RightHalf(), "PRF.Settings.Select".Translate() + " (" + options[currentValue] + ")"))
            {
                Find.WindowStack.Add(new FloatMenu(options.Select((o, idx) => new FloatMenuOption(o.ToString(), () => currentValue = idx)).ToList()));
            }

            list.Gap(list.verticalSpacing);
        }

        protected override string ReplaceText => options[value].ToString();

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;
            if (options == null || options.Count == 0)
            {
                Log.Error("invalid selectionList on Settings.xml");
                return false;
            }

            return true;
        }
    }
}