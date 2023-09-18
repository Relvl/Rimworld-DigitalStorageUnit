using RimWorld;
using System;
using UnityEngine;

namespace ProjectRimFactory.AutoMachineTool
{
    public class MoteProgressBar2 : MoteProgressBar
    {
        public override void Draw()
        {
            if (progressGetter != null)
            {
                progress = Mathf.Clamp01(progressGetter());
            }

            base.Draw();
        }

        public Func<float> progressGetter;
    }
}