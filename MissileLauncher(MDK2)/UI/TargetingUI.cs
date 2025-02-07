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
        public class TargetingUI
        {
            private static Dictionary<ControlStation, long> _selectedEntities = new Dictionary<ControlStation, long>();
            private ControlStation _controlStation;
            private Color _userColor;
            private TargetingSpriteBuilder _targetingSpriteBuilder;
            private IMyTextSurface _display;

            private int _runCounter = 0;

            private long _selectedEntityID = 0;
            private int _selectedSpriteIndex = 0;

            private List<Sprite3D> _entitySpriteList = new List<Sprite3D>();

            private SelectionMode _selectionMode = SelectionMode.None;
            private int _selectionModeIndex = 0;
            private int _numOfModes = 3;

            private static string _selectorSpriteName = "Selector";
            private static Vector2 _selectorSpriteNativeSize = new Vector2(512, 512);
            private Color _selectorSpriteColor;

            public enum SelectionMode
            {
                None, Target, Missile
            }

            public TargetingUI(IMyTextSurface display, TargetingSpriteBuilder targetingSpriteBuilder, ControlStation controlStation, Color userColor)
            {
                _display = display;
                _targetingSpriteBuilder = targetingSpriteBuilder;
                _controlStation = controlStation;
                _userColor = userColor;
                _selectorSpriteColor = _userColor;

                SetupDrawSurface(_display);
            }

            public void Run(DateTime time)
            {
                _runCounter++;
                _runCounter %= 10;

                if (_runCounter == 9)
                {
                    var frame = _display.DrawFrame();
                    DrawSprites(_display.TextureSize, frame);
                    frame.Dispose();
                }
            }

            public void CycleSelectionMode()
            {
                _selectionModeIndex++;
                _selectionModeIndex %= _numOfModes;
                _selectionMode = (SelectionMode)_selectionModeIndex;

                _selectedSpriteIndex = 0;
                _selectedEntityID = 0;
                
                if (_selectionMode == SelectionMode.Target)
                {
                    _entitySpriteList = _targetingSpriteBuilder.TargetSprites;
                }
                else if (_selectionMode == SelectionMode.Missile)
                {
                    _entitySpriteList = _targetingSpriteBuilder.MissileSprites;
                }
                else if (_selectionMode == SelectionMode.None)
                {
                    _entitySpriteList = null;
                }
                else
                {
                    _selectionMode = SelectionMode.None;
                }
            }

            public void SelectNextEntity()
            {
                if (_entitySpriteList?.Any() ?? false)
                {
                    _selectedSpriteIndex = _entitySpriteList.FindIndex(x => x.EntityID == _selectedEntityID);
                    _selectedSpriteIndex++;
                    _selectedSpriteIndex %= _entitySpriteList.Count;

                    _selectedEntityID = _entitySpriteList[_selectedSpriteIndex].EntityID;
                }
            }

            public void SelectPreviousEntity()
            {
                if (_entitySpriteList?.Any() ?? false)
                {
                    _selectedSpriteIndex = _entitySpriteList.FindIndex(x => x.EntityID == _selectedEntityID);
                    _selectedSpriteIndex--;
                    _selectedSpriteIndex %= _entitySpriteList.Count;

                    _selectedEntityID = _entitySpriteList[_selectedSpriteIndex].EntityID;
                }
            }

            public void SetupDrawSurface(IMyTextSurface surface)
            {
                // Draw background color
                surface.ScriptBackgroundColor = new Color(0, 0, 0, 255);

                // Set content type
                surface.ContentType = ContentType.SCRIPT;

                // Set script to none
                surface.Script = "";
            }

            public void DrawSprites(Vector2 screenSize, MySpriteDrawFrame frame)
            {
                foreach (var sprite in _targetingSpriteBuilder.FinalSprites)
                {
                    frame.Add(sprite.ToMySprite(screenSize));
                }

                if (_entitySpriteList?.Any() ?? false)
                {
                    Sprite3D selectedEntitySprite = _entitySpriteList[_selectedSpriteIndex];
                    var selectorScale = selectedEntitySprite.Scale;
                    var selectorPos = selectedEntitySprite.Position;
                    var selectorEntityID = selectedEntitySprite.EntityID;
                    Sprite3D selectorSprite = Sprite3D.CreateSprite3D(Sprite3D.Sprite3DType.Selector, _selectorSpriteName, selectorEntityID, selectorPos, _selectorSpriteNativeSize, selectorScale, 0, _selectorSpriteColor);
                    frame.Add(selectorSprite.ToMySprite(screenSize));
                }
            }
        }
    }
}
