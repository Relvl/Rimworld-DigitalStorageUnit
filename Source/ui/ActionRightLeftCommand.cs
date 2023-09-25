using System;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.ui;

internal class ActionRightLeftCommand : Command
{
    private static bool _wasRightClick;
    public Action ActionL;
    public Action ActionR;

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        if (_wasRightClick) ActionR();
        else ActionL();
    }

    public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
    {
        base.DrawIcon(rect, buttonMat, parms);
        if (Input.GetMouseButtonDown(0)) _wasRightClick = false;
        if (Input.GetMouseButtonDown(1)) _wasRightClick = true;
    }
}