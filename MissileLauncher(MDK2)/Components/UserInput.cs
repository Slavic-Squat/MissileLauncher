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
        public class UserInput
        {
            private IMyShipController _inputBlock;
            private DateTime _time;
            private DateTime _lastRunTime;

            public bool WPress { get; private set; } = false;
            private float _secondsWPressed;
            public bool WHeld { get; private set; } = false;
            public bool WRelease { get; private set; } = false;
            public bool WHeldAndReleased { get; private set; } = false;

            public bool APress { get; private set; } = false;
            private float _secondsAPressed;
            public bool AHeld { get; private set; } = false;
            public bool ARelease { get; private set; } = false;
            public bool AHeldAndReleased { get; private set; } = false;

            public bool SPress { get; private set; } = false;
            private float _secondsSPressed;
            public bool SHeld { get; private set; } = false;
            public bool SRelease { get; private set; } = false;
            public bool SHeldAndReleased { get; private set; } = false;

            public bool DPress { get; private set; } = false;
            private float _secondsDPressed;
            public bool DHeld { get; private set; } = false;
            public bool DRelease { get; private set; } = false;
            public bool DHeldAndReleased { get; private set; } = false;

            public bool CPress { get; private set; } = false;
            private float _secondsCPressed;
            public bool CHeld { get; private set; } = false;
            public bool CRelease { get; private set; } = false;
            public bool CHeldAndReleased { get; private set; } = false;

            public bool SpacePress { get; private set; } = false;
            private float _secondsSpacePressed;
            public bool SpaceHeld { get; private set; } = false;
            public bool SpaceRelease { get; private set; } = false;
            public bool SpaceHeldAndReleased { get; private set; } = false;

            public bool QPress { get; private set; } = false;
            private float _secondsQPressed;
            public bool QHeld { get; private set; } = false;
            public bool QRelease { get; private set; } = false;
            public bool QHeldAndReleased { get; private set; } = false;

            public bool EPress { get; private set; } = false;
            private float _secondsEPressed;
            public bool EHeld { get; private set; } = false;
            public bool ERelease { get; private set; } = false;
            public bool EHeldAndReleased { get; private set; } = false;

            public Vector2 MouseInput { get; private set; } = Vector2.Zero;

            public bool Press1
            {
                get
                {
                    return _press1;
                }
                private set
                {
                    _press1 = value;
                    _press1Set = true;
                }
            }
            private bool _press1 = false;
            private bool _press1Set = false;
            public bool Press2
            {
                get
                {
                    return _press2;
                }
                private set
                {
                    _press2 = value;
                    _press2Set = true;
                }
            }
            private bool _press2 = false;
            private bool _press2Set = false;
            public bool Press3
            {
                get
                {
                    return _press3;
                }
                private set
                {
                    _press3 = value;
                    _press3Set = true;
                }
            }
            private bool _press3 = false;
            private bool _press3Set = false;
            public bool Press4
            {
                get
                {
                    return _press4;
                }
                private set
                {
                    _press4 = value;
                    _press4Set = true;
                }
            }
            private bool _press4 = false;
            private bool _press4Set = false;
            public bool Press5
            {
                get
                {
                    return _press5;
                }
                private set
                {
                    _press5 = value;
                    _press5Set = true;
                }
            }
            private bool _press5 = false;
            private bool _press5Set = false;
            public bool Press6
            {
                get
                {
                    return _press6;
                }
                private set
                {
                    _press6 = value;
                    _press6Set = true;
                }
            }
            private bool _press6 = false;
            private bool _press6Set = false;
            public bool Press7
            {
                get
                {
                    return _press7;
                }
                private set
                {
                    _press7 = value;
                    _press7Set = true;
                }
            }
            private bool _press7 = false;
            private bool _press7Set = false;
            public bool Press8
            {
                get
                {
                    return _press8;
                }
                private set
                {
                    _press8 = value;
                    _press8Set = true;
                }
            }
            private bool _press8 = false;
            private bool _press8Set = false;
            public bool Press9
            {
                get
                {
                    return _press9;
                }
                private set
                {
                    _press9 = value;
                    _press9Set = true;
                }
            }
            private bool _press9 = false;
            private bool _press9Set = false;

            public UserInput(IMyShipController inputBlock)
            {
                _inputBlock = inputBlock;
            }

            public void Run(DateTime time)
            {
                ListenForInput(time);
                HandleNumbers();
            }

            public void ListenForInput(DateTime time)
            {
                if (_time == DateTime.MinValue)
                {
                    _time = time;
                }
                _lastRunTime = _time;
                _time = time;

                float deltaSeconds = (float)(_time - _lastRunTime).TotalSeconds;


                if (_inputBlock.MoveIndicator.Z < 0)
                {
                    WPress = true;
                    _secondsWPressed += deltaSeconds;

                    if (_secondsWPressed > 1)
                    {
                        WHeld = true;
                    }
                }
                else
                {
                    if (WHeld == true)
                    {
                        WHeldAndReleased = true;
                    }
                    else if (WPress == true)
                    {
                        WRelease = true;
                    }
                    else
                    {
                        WRelease = false;
                        WHeldAndReleased = false;
                    }
                    WPress = false;
                    WHeld = false;
                    _secondsWPressed = 0;
                }

                if (_inputBlock.MoveIndicator.Z > 0)
                {
                    SPress = true;
                    _secondsSPressed += deltaSeconds;

                    if (_secondsSPressed > 1)
                    {
                        SHeld = true;
                    }
                }
                else
                {
                    if (SHeld == true)
                    {
                        SHeldAndReleased = true;
                    }
                    else if (SPress == true)
                    {
                        SRelease = true;
                    }
                    else
                    {
                        SRelease = false;
                        SHeldAndReleased = false;
                    }
                    SPress = false;
                    SHeld = false;
                    _secondsSPressed = 0;
                }

                if (_inputBlock.MoveIndicator.X < 0)
                {
                    APress = true;
                    _secondsAPressed += deltaSeconds;

                    if (_secondsAPressed > 1)
                    {
                        AHeld = true;
                    }
                }
                else
                {
                    if (AHeld == true)
                    {
                        AHeldAndReleased = true;
                    }
                    else if (APress == true)
                    {
                        ARelease = true;
                    }
                    else
                    {
                        ARelease = false;
                        AHeldAndReleased = false;
                    }
                    APress = false;
                    AHeld = false;
                    _secondsAPressed = 0;
                }

                if (_inputBlock.MoveIndicator.X > 0)
                {
                    DPress = true;
                    _secondsDPressed += deltaSeconds;

                    if (_secondsDPressed > 1)
                    {
                        DHeld = true;
                    }
                }
                else
                {
                    if (DHeld == true)
                    {
                        DHeldAndReleased = true;
                    }
                    else if (DPress == true)
                    {
                        DRelease = true;
                    }
                    else
                    {
                        DRelease = false;
                        DHeldAndReleased = false;
                    }
                    DPress = false;
                    DHeld = false;
                    _secondsDPressed = 0;
                }

                if (_inputBlock.MoveIndicator.Y < 0)
                {
                    CPress = true;
                    _secondsCPressed += deltaSeconds;

                    if (_secondsCPressed > 0.5f)
                    {
                        CHeld = true;
                    }
                }
                else
                {
                    if (CHeld == true)
                    {
                        CHeldAndReleased = true;
                    }
                    else if (CPress == true)
                    {
                        CRelease = true;
                    }
                    else
                    {
                        CRelease = false;
                        CHeldAndReleased = false;
                    }
                    CPress = false;
                    CHeld = false;
                    _secondsCPressed = 0;
                }

                if (_inputBlock.MoveIndicator.Y > 0)
                {
                    SpacePress = true;
                    _secondsSpacePressed += deltaSeconds;

                    if (_secondsSpacePressed > 1)
                    {
                        SpaceHeld = true;
                    }
                }
                else
                {
                    if (SpaceHeld == true)
                    {
                        SpaceHeldAndReleased = true;
                    }
                    else if (SpacePress == true)
                    {
                        SpaceRelease = true;
                    }
                    else
                    {
                        SpaceRelease = false;
                        SpaceHeldAndReleased = false;
                    }
                    SpacePress = false;
                    SpaceHeld = false;
                    _secondsSpacePressed = 0;
                }

                if (_inputBlock.RollIndicator < 0)
                {
                    QPress = true;
                    _secondsQPressed += deltaSeconds;

                    if (_secondsQPressed > 1)
                    {
                        QHeld = true;
                    }
                }
                else
                {
                    if (QHeld == true)
                    {
                        QHeldAndReleased = true;
                    }
                    else if (QPress == true)
                    {
                        QRelease = true;
                    }
                    else
                    {
                        QRelease = false;
                        QHeldAndReleased = false;
                    }
                    QPress = false;
                    QHeld = false;
                    _secondsQPressed = 0;
                }

                if (_inputBlock.RollIndicator > 0)
                {
                    EPress = true;
                    _secondsEPressed += deltaSeconds;

                    if (_secondsEPressed > 1)
                    {
                        EHeld = true;
                    }
                }
                else
                {
                    if (EHeld == true)
                    {
                        EHeldAndReleased = true;
                    }
                    else if (EPress == true)
                    {
                        ERelease = true;
                    }
                    else
                    {
                        ERelease = false;
                        EHeldAndReleased = false;
                    }
                    EPress = false;
                    EHeld = false;
                    _secondsEPressed = 0;
                }

                MouseInput = _inputBlock.RotationIndicator;
            }

            public void HandleNumbers()
            {
                if (Press1 == true && _press1Set == false)
                {
                    Press1 = false;
                }
                if (Press2 == true && _press2Set == false)
                {
                    Press2 = false;
                }
                if (Press3 == true && _press3Set == false)
                {
                    Press3 = false;
                }
                if (Press4 == true && _press4Set == false)
                {
                    Press4 = false;
                }
                if (Press5 == true && _press5Set == false)
                {
                    Press5 = false;
                }
                if (Press6 == true && _press6Set == false)
                {
                    Press6 = false;
                }
                if (Press7 == true && _press7Set == false)
                {
                    Press7 = false;
                }
                if (Press8 == true && _press8Set == false)
                {
                    Press8 = false;
                }
                if (Press9 == true && _press9Set == false)
                {
                    Press9 = false;
                }

                _press1Set = false;
                _press2Set = false;
                _press3Set = false;
                _press4Set = false;
                _press5Set = false;
                _press6Set = false;
                _press7Set = false;
                _press8Set = false;
                _press9Set = false;
            }

            public void PressNumber(string numberString)
            {
                int number = 0;
                int.TryParse(numberString, out number);

                switch (number)
                {
                    case 1:
                        Press1 = true;
                        break;
                    case 2:
                        Press2 = true;
                        break;
                    case 3:
                        Press3 = true;
                        break;
                    case 4:
                        Press4 = true;
                        break;
                    case 5:
                        Press5 = true;
                        break;
                    case 6:
                        Press6 = true;
                        break;
                    case 7:
                        Press7 = true;
                        break;
                    case 8:
                        Press8 = true;
                        break;
                    case 9:
                        Press9 = true;
                        break;
                }
            }
        }
    }
}
