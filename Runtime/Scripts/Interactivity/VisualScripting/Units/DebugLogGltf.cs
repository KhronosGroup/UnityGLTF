using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    [UnitCategory("Debug")]
    [UnitTitle("Debug Log glTF (Message, Args)")]
    [TypeIcon(typeof(Debug))]
    public class DebugLogGltf : Unit
    {
        public enum LogVerbosity
        {
            Log = 0,
            Warning = 1,
            Error = 2
        }
        
        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Verbosity")]
        public LogVerbosity logVerbosity
        {
            get => _logVerbosity;
            set => _logVerbosity = value;
        }
        
        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Message")]
        public string message
        {
            get => _message;
            set => _message = value;
        }
        
        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Arguments")]
        public int argumentCount
        {
            get => _argumentCount;
            set => _argumentCount = Mathf.Clamp(value, 0, 20);
        }

        [SerializeAs(nameof(LogVerbosity))]
        private LogVerbosity _logVerbosity = LogVerbosity.Log;
        
        [SerializeAs(nameof(argumentCount))]
        private int _argumentCount = 1;
        
        [SerializeAs(nameof(message))]
        private string _message = "Output: {0}";
        
        
        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput enter { get; private set; }
       
        [DoNotSerialize]
        public List<ValueInput> argumentPorts { get; } = new List<ValueInput>();
        
        [DoNotSerialize]
        [PortLabelHidden]
        public ControlOutput exit { get; private set; }

        protected override void Definition()
        {
            enter = ControlInput("enter", (flow) =>
            {
                AssignArguments(flow, out var args);

                switch (logVerbosity)
                {
                    case LogVerbosity.Log:
                        Debug.LogFormat(message, args);
                        break;
                    case LogVerbosity.Warning:
                        Debug.LogWarningFormat(message, args);
                        break;
                    case LogVerbosity.Error:
                        Debug.LogErrorFormat(message, args);
                        break;
                }
                return exit;
            });

            exit = ControlOutput("exit");
            Succession(enter, exit);
            
            argumentPorts.Clear();

            for (var i = 0; i < argumentCount; i++)
            {
                var port = ValueInput<object>($"Arg. - {i}");
                argumentPorts.Add(port);
                Requirement(port, enter);
            }
        }
        
        protected void AssignArguments(Flow flow, out object[] args)
        {
            args = new object[argumentCount];
            for (var i = 0; i < argumentCount; i++)
            {
                args[i] = flow.GetValue(argumentPorts[i]);
            }
        }
    }
}