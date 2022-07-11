﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    class Command_ActionRightLeft : Command
    {

		private static bool wasRightClick = false;

		public Action actionL;
		public Action actionR;

		private Color? iconDrawColorOverride;

		public override Color IconDrawColor => iconDrawColorOverride ?? base.IconDrawColor;

		public override void ProcessInput(Event ev)
		{
			//Log.Message($"{ev} -  {ev.keyCode} - {ev.isMouse}");
			base.ProcessInput(ev);
            if(wasRightClick)
            {
				actionR();
			}
            else
            {
				actionL();
			}
			
		}

        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            base.DrawIcon(rect, buttonMat, parms);

			if (Input.GetMouseButtonDown(0))
            {
				wasRightClick = false;
			}
			if (Input.GetMouseButtonDown(1))
            {
				wasRightClick = true;
			}
		}

        public void SetColorOverride(Color color)
		{
			iconDrawColorOverride = color;
		}

	}
}
