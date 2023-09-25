using System;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.ui;

public class OutputMinMaxDialog : Window
{
    private enum LimitUpdateRequestFocus
    {
        Max = 1,
        Min = 0,
        Undefined = -1
    }

    private static class LimitUpdateRequest
    {
        private const int GracePeriodMax = 75; // todo! max stacks

        private static int _gracePeriod = -1;
        private static string _lastval = "";
        private static Predicate<bool> _predicate = _ => false;

        private static OutputMinMaxDialog _parent;

        /// <summary>
        /// True  => min = max;
        /// false => max = min;
        /// </summary>
        private static bool _minIsMax;

        public static void Init(OutputMinMaxDialog par, Predicate<bool> validator)
        {
            _predicate = validator;
            _parent = par;
        }

        public static void Update(bool dir, string buff)
        {
            if (_gracePeriod == -1 || buff != _lastval)
                _gracePeriod = GracePeriodMax;
            else if (dir != _minIsMax)
                LostFocus();

            _minIsMax = dir;
            _lastval = buff;
        }

        public static void CheckFocusLoss(LimitUpdateRequestFocus maxFocus)
        {
            if (_gracePeriod == -1) return;
            if (maxFocus < 0 || (((int)maxFocus != 1 || !_minIsMax) && ((int)maxFocus != 0 || _minIsMax))) LostFocus();
        }

        public static void LostFocus(bool force = false)
        {
            if (_gracePeriod == -1 && !force) return;
            _gracePeriod = 1;
            Tick();
        }

        public static void Tick()
        {
            if (_gracePeriod >= 0) _gracePeriod--;
            if (_gracePeriod == 0 && _predicate(_minIsMax))
                _parent?.OverrideBuffer(_minIsMax);
        }
    }

    private void OverrideBuffer(bool minIsMax)
    {
        if (minIsMax)
        {
            _minBufferString = _maxBufferString;
        }
        else
        {
            _maxBufferString = _minBufferString;
        }
    }

    private string _controlIdMinInput;
    private string _controlIdMaxInput;

    private readonly OutputSettings _outputSettings;
    private readonly Action _postClose;

    private const float TitleLabelHeight = 32f;
    private string _minBufferString;
    private string _maxBufferString;

    public OutputMinMaxDialog(OutputSettings settings, Action postClose = null)
    {
        _outputSettings = settings;
        _postClose = postClose;
        doCloseX = true;
        doCloseButton = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
        draggable = true;
        drawShadow = true;
        focusWhenOpened = true;
        forcePause = true;
    }

    public override Vector2 InitialSize => new(500f, 250f);

    private bool Validator(bool data)
    {
        if (_maxBufferString.NullOrEmpty())
        {
            _maxBufferString = "0";
            _outputSettings.Max = 0;
        }

        if (_minBufferString.NullOrEmpty())
        {
            _minBufferString = "0";
            _outputSettings.Min = 0;
        }

        return _outputSettings.Max < _outputSettings.Min;
    }

    public override void DoWindowContents(Rect rect)
    {
        _maxBufferString ??= _outputSettings.Max.ToString();
        _minBufferString ??= _outputSettings.Min.ToString();

        var list = new Listing_Standard(GameFont.Small) { ColumnWidth = rect.width };

        var focus = GUI.GetNameOfFocusedControl();
        if (focus == _controlIdMaxInput)
            LimitUpdateRequest.CheckFocusLoss(LimitUpdateRequestFocus.Max);
        else if (focus == _controlIdMinInput)
            LimitUpdateRequest.CheckFocusLoss(LimitUpdateRequestFocus.Min);
        else
            LimitUpdateRequest.CheckFocusLoss(LimitUpdateRequestFocus.Undefined);

        list.Begin(rect);
        var titleRect = new Rect(0f, 0f, rect.width, TitleLabelHeight);
        Text.Font = GameFont.Medium;
        Widgets.Label(titleRect, "DSU.MinMaxAmounts".Translate());
        Text.Font = GameFont.Small;
        list.Gap();
        list.Gap();
        list.Gap();
        list.CheckboxLabeled("DSU.UseMin".Translate(), ref _outputSettings.UseMin, _outputSettings.MinTooltip.Translate());

        list.Gap();
        {
            var rectLine = list.GetRect(Text.LineHeight);
            var rectLeft = rectLine.LeftHalf().Rounded();
            var rectRight = rectLine.RightHalf().Rounded();
            var anchorBuffer = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.DrawHighlightIfMouseover(rectLine);
            Widgets.Label(rectLeft, "DSU.Min".Translate());
            Text.Anchor = anchorBuffer;
            Widgets.TextFieldNumeric(rectRight, ref _outputSettings.Min, ref _minBufferString);
            _controlIdMinInput ??= "TextField" + rectRight.y.ToString("F0") + rectRight.x.ToString("F0");
        }
        if (_outputSettings.Max < _outputSettings.Min && GUI.GetNameOfFocusedControl() == _controlIdMinInput)
        {
            LimitUpdateRequest.Update(false, _minBufferString);
        }

        list.Gap();
        list.CheckboxLabeled("DSU.UseMax".Translate(), ref _outputSettings.UseMax, _outputSettings.MaxTooltip.Translate());

        list.Gap();
        {
            var rectLine = list.GetRect(Text.LineHeight);
            var rectLeft = rectLine.LeftHalf().Rounded();
            var rectRight = rectLine.RightHalf().Rounded();
            var anchorBuffer = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.DrawHighlightIfMouseover(rectLine);
            Widgets.Label(rectLeft, "DSU.Max".Translate());
            Text.Anchor = anchorBuffer;
            Widgets.TextFieldNumeric(rectRight, ref _outputSettings.Max, ref _maxBufferString);
            _controlIdMaxInput ??= "TextField" + rectRight.y.ToString("F0") + rectRight.x.ToString("F0");
        }

        if (_outputSettings.Min > _outputSettings.Max && GUI.GetNameOfFocusedControl() == _controlIdMaxInput)
        {
            LimitUpdateRequest.Update(true, _maxBufferString);
        }

        LimitUpdateRequest.Tick();

        list.End();
    }

    public override void PostClose()
    {
        LimitUpdateRequest.LostFocus(_maxBufferString.NullOrEmpty() || _minBufferString.NullOrEmpty());
        base.PostClose();
        _postClose?.Invoke();
    }

    public override void PreOpen()
    {
        LimitUpdateRequest.Init(this, Validator);
        base.PreOpen();
    }
}