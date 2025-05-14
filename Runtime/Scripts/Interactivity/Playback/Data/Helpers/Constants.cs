namespace UnityGLTF.Interactivity.Playback
{
    public static class Constants
    {
        public const int UNCONNECTED_NODE_INDEX = -1;
        public const int INVALID_TYPE_INDEX = -1;
        public const string EMPTY_SOCKET_STRING = "";
    }

    public static class Pointers
    {
        public const string ANIMATIONS_LENGTH = "animations.length";
        public const string MATERIALS_LENGTH = "materials.length";
        public const string MESHES_LENGTH = "meshes.length";
        public const string NODES_LENGTH = "nodes.length";
        public const string WEIGHTS_LENGTH = "weights.length";
        public const string WEIGHTS = "weights";
        public const string EXTENSIONS = "extensions";
        public const string TRANSLATION = "translation";
        public const string ROTATION = "rotation";
        public const string SCALE = "scale";
        public const string MATRIX = "matrix";
        public const string GLOBAL_MATRIX = "globalMatrix";
        public const string IS_PLAYING = "isPlaying";
        public const string PLAYHEAD = "playhead";
        public const string VIRTUAL_PLAYHEAD = "virtualPlayhead";
        public const string MIN_TIME = "minTime";
        public const string MAX_TIME = "maxTime";

    }

    public static class ConstStrings
    {
        public const string REMAINING_INPUTS = "remainingInputs";
        public const string LAST_INDEX = "lastIndex";
        public const string LAST_REMAINING_TIME = "lastRemainingTime";
        public const string TIME_SINCE_START = "timeSinceStart";
        public const string TIME_SINCE_LAST_TICK = "timeSinceLastTick";
        public const string EXPECTED = "expected";
        public const string ACTUAL = "actual";
        public const string SEVERITY = "severity";
        public const string MESSAGE = "message";
        public const string NODE_INDEX = "nodeIndex";
        public const string DEFAULT = "default";
        public const string SELECTION = "selection";
        public const string CASES = "cases";
        public const string GRAPHS = "graphs";
        public const string GRAPH = "graph";
        public const string EXTENSION = "extension";
        public const string OP = "op";
        public const string INPUT_VALUE_SOCKETS = "inputValueSockets";
        public const string OUTPUT_VALUE_SOCKETS = "outputValueSockets";
        public const string DECLARATIONS = "declarations";
        public const string DECLARATION = "declaration";
        public const string EVENTS = "events";
        public const string EVENT = "event";
        public const string NODES = "nodes";
        public const string NODE = "node";
        public const string SOCKET = "socket";
        public const string SIGNATURE = "signature";
        public const string CONDITION = "condition";
        public const string TRUE = "true";
        public const string FALSE = "false";
        public const string POINTER = "pointer";
        public const string COMPLETED = "completed";
        public const string LOOP_BODY = "loopBody";
        public const string START_INDEX = "startIndex";
        public const string END_INDEX = "endIndex";
        public const string LAST_DELAY_INDEX = "lastDelayIndex";
        public const string DELAY_INDEX = "delayIndex";
        public const string INITIAL_INDEX = "initialIndex";
        public const string INDEX = "index";
        public const string ANIMATION = "animation";
        public const string SPEED = "speed";
        public const string START_TIME = "startTime";
        public const string STOP_TIME = "stopTime";
        public const string END_TIME = "endTime";
        public const string CANCEL = "cancel";
        public const string CURRENT_COUNT = "currentCount";

        public const string VARIABLES = "variables";
        public const string VARIABLE = "variable";

        public const string TYPES = "types";
        public const string IS_VALID = "isValid";

        public const string VALUE = "value";
        public const string VALUES = "values";
        public const string ID = "id";
        public const string NAME = "name";
        public const string TYPE = "type";
        public const string DESCRIPTION = "description";
        public const string METADATA = "metadata";
        public const string CONFIGURATION = "configuration";
        public const string FLOWS = "flows";
        public const string INPUT_FLOWS = "inputFlows";
        public const string OUTPUT_FLOWS = "outputFlows";
        public const string RESET = "reset";
        public const string IS_LOOP = "isLoop";
        public const string IS_RANDOM = "isRandom";

        public const string A = "a";
        public const string B = "b";
        public const string C = "c";
        public const string D = "d";
        public const string E = "e";
        public const string F = "f";
        public const string G = "g";
        public const string H = "h";
        public const string I = "i";
        public const string J = "j";
        public const string K = "k";
        public const string L = "l";
        public const string M = "m";
        public const string N = "n";
        public const string O = "o";
        public const string P = "p";

        public static readonly string[] Letters = new string[]
        {
            A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P
        };

        public static readonly string[] Numbers = new string[]
        {
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15",
            "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31",
            "32", "33", "34", "35", "36", "37", "38", "39", "40", "41", "42", "43", "44", "45", "46", "47",
            "48", "49", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "60", "61", "62", "63", "64"
        };

        public const string DONE = "done";
        public const string IN = "in";
        public const string OUT = "out";
        public const string ERR = "err";
        public const string P1 = "p1";
        public const string P2 = "p2";
        public const string DURATION = "duration";

        public const string TRANSLATION = "translation";
        public const string ROTATION = "rotation";
        public const string SCALE = "scale";
        public const string USE_SLERP = "useSlerp";

        public const string CONTROLLER_INDEX = "controllerIndex";
        public const string SELECTED_NODE_INDEX = "selectedNodeIndex";
        public const string SELECTION_POINT = "selectionPoint";
        public const string SELECTION_RAY_ORIGIN = "selectionRayOrigin";
        public const string STOP_PROPAGATION = "stopPropagation";
        public const string HOVER_NODE_INDEX = "hoverNodeIndex";
    }
}