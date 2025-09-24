using Sandbox.Game.AI;
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
        public class CommunicationHandler
        {
            public Program Program { get; private set; }
            public int ID { get; private set; }
            private HashSet<IMyBroadcastListener> _broadcastListeners = new HashSet<IMyBroadcastListener>();
            private IMyUnicastListener _unicastListener;
            private Dictionary<string, Queue<MyIGCMessage>> _messages = new Dictionary<string, Queue<MyIGCMessage>>();

            public CommunicationHandler(Program program, int iD)
            {
                Program = program;
                ID = iD;
                _unicastListener = Program.IGC.UnicastListener;
            }

            public void Recieve()
            {
                while (_unicastListener.HasPendingMessage)
                {
                    var message = _unicastListener.AcceptMessage();
                    if (message.Source != Program.IGC.Me && _messages.ContainsKey(message.Tag))
                    {
                        _messages[message.Tag].Enqueue(message);

                        if (_messages[message.Tag].Count > 20)
                        {
                            // Prevent memory overflow by limiting queue size
                            _messages[message.Tag].Dequeue();
                        }
                    }
                }

                foreach (var listener in _broadcastListeners)
                {
                    while (listener.HasPendingMessage)
                    {
                        var message = listener.AcceptMessage();
                        if (message.Source != Program.IGC.Me && _messages.ContainsKey(message.Tag))
                        {
                            _messages[message.Tag].Enqueue(message);

                            if (_messages[message.Tag].Count > 20)
                            {
                                // Prevent memory overflow by limiting queue size
                                _messages[message.Tag].Dequeue();
                            }
                        }
                    }
                }
            }

            public void SendBroadcast(byte[] data, string tag)
            {
                string dataString = Convert.ToBase64String(data);
                Program.IGC.SendBroadcastMessage(tag, dataString);
            }

            public void SendUnicast(byte[] data, long targetAddress, string tag)
            {
                string dataString = Convert.ToBase64String(data);
                Program.IGC.SendUnicastMessage(targetAddress, tag, dataString);
            }

            public void RegisterBroadcastListener(string tag)
            {
                var listener = Program.IGC.RegisterBroadcastListener(tag);
                _broadcastListeners.Add(listener);
                RegisterTag(tag);
            }

            public void UnregisterBroadcastListener(string tag)
            {
                _broadcastListeners.RemoveWhere(l => l.Tag == tag);
            }

            public void RegisterTag(string tag)
            {
                if (!_messages.ContainsKey(tag))
                {
                    _messages[tag] = new Queue<MyIGCMessage>();
                }
            }

            public void UnregisterTag(string tag)
            {
                _messages.Remove(tag);
            }

            public bool HasMessage(string tag)
            {
                return _messages.ContainsKey(tag) && _messages[tag].Count > 0;
            }

            public bool TryRetrieveMessage(string tag, out MyIGCMessage message)
            {
                if (HasMessage(tag))
                {
                    message = _messages[tag].Dequeue();
                    return true;
                }
                message = default(MyIGCMessage);
                return false;
            }
        }
    }
}
