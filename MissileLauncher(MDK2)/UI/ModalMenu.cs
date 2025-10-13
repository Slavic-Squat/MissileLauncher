using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;


namespace IngameScript
{
    partial class Program
    {
        public class ModalMenu : Menu, IModal
        {
            public bool CanClose => _canClose?.Invoke() ?? true;

            private Func<bool> _canClose;

            public ModalMenu(Vector2 pos, Vector2 size, float borderThickness, Func<bool> canClose, bool obscure = false, IMyTextSurface surface = null, Func<bool> autoClose = null) : base(pos, size, borderThickness, obscure, surface, autoClose)
            {
                _canClose = canClose;
            }

            protected override void Close()
            {
                if (CanClose == false)
                {
                    return;
                }
                base.Close();
            }
        }
    }
}
