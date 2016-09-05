#region usings
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Local
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "WebSocketTestClient", 
        Category = "Value", 
        Help = "Websocket clinet for Emotional intellect", 
        Tags = "",
        AutoEvaluate = true
        )]
	#endregion PluginInfo
	public class ValueWebSocketTestClientNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
	{
		#region pins
		[Input("Server URI", DefaultString = "ws://127.0.0.1:5000/ws", IsSingle = true)]
		public IDiffSpread<string> FinUri;

        [Input("Say something(For debug)", DefaultString = "Hello", IsSingle = true)]
        public IDiffSpread<string> FInHello;

        [Input("Test switch on", DefaultBoolean = false, IsSingle = true)]
        public IDiffSpread<bool> FInFinished;

        [Output("In connected")]
		public ISpread<bool> FOutInsConnected;

	    [Output("Сharacter ID (0..7)")]
        public ISpread<int> FOutAge;

        [Output("Current state (For debug)", IsSingle = true)]
        public ISpread<string> FOutState;
        #endregion pins

        #region fields

        [Import()]
        public ILogger FLogger;

	    private volatile StateEn FState;
	    private volatile string FMessage;
	    private WebSocketWrapper FSocket;
        private Stopwatch FStopwatch = new Stopwatch();

        #endregion fields

        #region utils
        enum StateEn
        {
            SETUP,
            CONNECTION,
            INIT,
            SWITCH_ON,
            WAITING_RESULTS,
            READY,
            DISCONECTED,
            RESET
        }
        #endregion utils


        #region constructor & destructor
        public ValueWebSocketTestClientNode()
        {
            FState = StateEn.SETUP;
            FMessage = null;
        }

        public void OnImportsSatisfied()
        {
            FOutInsConnected.SliceCount = 1;
            FOutInsConnected[0] = false;
            FOutAge.SliceCount = 0;
            FStopwatch.Reset();
            FStopwatch.Start();
        }

        public void Dispose()
        {
            FSocket.Close();
            FLogger.Log(LogType.Debug, "[WebSocketTestClient] Destructor");
        }

        #endregion constructor & destructor

#region mainloop
        public void Evaluate(int SpreadMax)
        {
            FOutState[0] = FState.ToString();
            ////////// Delete it
            if (FInHello.IsChanged)
            {
                FSocket?.SendMessage(FInHello[0]);
            }
            /////////////////

            if (   FState != StateEn.CONNECTION
                && FState != StateEn.DISCONECTED
                && FState != StateEn.RESET
                && FState != StateEn.SETUP)
            {
                if (FStopwatch?.Elapsed.Seconds >= 30)
                {
                    FLogger.Log(LogType.Debug, "[WebSocketTestClient] Ping.");
                    FStopwatch.Stop();
                    FStopwatch.Reset();
                    FStopwatch.Start();
                    SendReq("ping");
                }
            }

            string msg = FMessage;
            FMessage = null;

            if (msg!=null && CheckReset(msg))
            {
                SendResp("reset", "success", "Ok");
                FState = StateEn.RESET;
                return;
            }

            if (msg != null &&
                (FState == StateEn.READY))
            {
                SendReq("error");
            }

            switch (FState)
            {
                case StateEn.SETUP:
                    Task.Run(() => MakeConnection());
                    FState = StateEn.CONNECTION;
                    break;
                case StateEn.CONNECTION:
                    break;
                case StateEn.INIT:
                    if (msg != null)
                    {
                        if (CheckResp(StateEn.INIT, msg))
                        {
                            FState = StateEn.SWITCH_ON;
                            FOutAge.SliceCount = 0;
                            SendReq("switch_on");
                        }
                        else
                        {
                            SendReq("error");
                        }
                    }
                    break;
                case StateEn.SWITCH_ON:
                    if (msg != null)
                    {
                        if (CheckResp(StateEn.SWITCH_ON, msg))
                        {
                            FState = StateEn.WAITING_RESULTS;
                        }
                        else
                        {
                            SendReq("error");
                        }
                    }
                    break;
                case StateEn.WAITING_RESULTS:
                    if (msg != null)
                    {
                        int pers = ParseResults(msg);
                        if (pers == -1)
                        {
                            SendResp("error", "failed", "Некорректный ответ");
                            FOutAge.SliceCount = 0;
                        }
                        else
                        {
                            SendResp("results", "success", "Результаты получены");
                            FState = StateEn.READY;
                            FOutAge.SliceCount = 1;
                            FOutAge[0] = pers;
                        }
                    }
                    break;
                case StateEn.READY:
                    ////////// Del it
                    if (FInHello.IsChanged)
                    {
                        FSocket?.SendMessage(FInHello[0]);
                    }
                    /////////////////
                    if (FInFinished[0])
                    {
                        SendReq("switch_on");
                        FState = StateEn.SWITCH_ON;
                        FOutAge.SliceCount = 0;
                    }
                    break;
                case StateEn.DISCONECTED:
                    if (FInFinished[0])
                    {
                        FState = StateEn.SETUP;
                    }
                    break;
                case StateEn.RESET:
                    if (FInFinished[0])
                    {
                        SendReq("init");
                        FState = StateEn.INIT;
                    }
                    break;
                default:
                    FLogger.Log(LogType.Error, "Illegal state.");
                    break;
            }
        }
        #endregion mainloop
        
	    void MakeConnection()
	    {
            FLogger.Log(LogType.Debug, "[WebSocketTestClient] Creating connecton.");
            FSocket = WebSocketWrapper.Create(FinUri[0]);
            FSocket.OnConnect(OnConnect);
            FSocket.OnDisconnect(OnDisconnect);
            FSocket.OnMessage(OnMessage);
	        FSocket.Connect();
        }

        void OnConnect(WebSocketWrapper wrapper)
        {
            FLogger.Log(LogType.Debug, "[WebSocketTestClient] Connected.");
            FOutInsConnected.SliceCount = 1;
            FOutInsConnected[0] = true;

            SendReq("init");
            FState = StateEn.INIT;
        }

        void OnDisconnect(WebSocketWrapper wrapper)
        {
            FLogger.Log(LogType.Debug, "[WebSocketTestClient] Disconnected.");
            FOutInsConnected.SliceCount = 1;
            FOutInsConnected[0] = false;
            FState = StateEn.DISCONECTED;
        }

        void OnMessage(string msg, WebSocketWrapper wrapper)
        {
            if (FMessage != null)
            {
                FLogger.Log(LogType.Debug,
                    $"[WebSocketTestClient] Incoming message [{msg}] ignored. Not processed previus.");
                SendReq("error");
            }
            else
            {
                FLogger.Log(LogType.Debug, $"[WebSocketTestClient] Incoming message [{msg}]");
                FMessage = msg;
            }
        }

	    void SendReq(string req)
	    {
            int unixTime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            string msg = $"{{\"timestamp\": \"{unixTime}\", \"device_id\": \"bot\",\"msg_type\": \"{req}\"}}";
            FSocket?.SendMessage(msg);
            FLogger.Log(LogType.Debug, $"[WebSocketTestClient] Sendig message: {msg}");
        }

        private void SendResp(string req, string status, string message)
        {
            int unixTime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            string msg = $"{{\"timestamp\": \"{unixTime}\", \"device_id\": \"bot\",\"msg_type\": \"{req}\",\"status\": \"{status}\",\"msg\": \"{message}\"}}";
            FSocket?.SendMessage(msg);
            FLogger.Log(LogType.Debug, $"[WebSocketTestClient] Sendig message: {msg}");
        }

        private bool CheckReset(string msg)
        {
            JObject jObject;
            try
            {
                jObject = JObject.Parse(msg);
            }
            catch (Exception)
            {
                return false;
            }
            var t_type = jObject.GetValue("msg_type");
            if (t_type != null && t_type.Value<string>() == "reset")
                return true;
            return false;
        }

        bool CheckResp(StateEn state, string msg)
        {
            string reqType;
            if (state == StateEn.INIT)
                reqType = "init";
            else if (state == StateEn.SWITCH_ON)
                reqType = "switch_on";
            else
                return false;
            JObject jObject;
            try
            {
                jObject = JObject.Parse(msg);
            }
            catch (Exception)
            {
                return false;
            }

            var t_type = jObject.GetValue("msg_type");
            var t_st = jObject.GetValue("status");
 
            if (t_st == null || t_type == null)
                return false;

            string msg_type = t_type.Value<string>();
            string status = t_st.Value<string>();
            FLogger.Log(LogType.Debug, $"[WebSocketTestClient] Response parsed. Message: [{msg}]. msg_type: {msg_type}, status: {status}");
            if (msg_type != reqType || status != "success")
                return false;
            return true;
        }

        int ParseResults(string msg)
        {
            JObject jObject;
            try
            {
                jObject = JObject.Parse(msg);
            }
            catch (Exception)
            {
                return -1;
            }
            var t_type_f = jObject.GetValue("msg_type");
            if (t_type_f == null || t_type_f.Value<string>() != "results")
                return -1;
            var gender = jObject.GetValue("gender");
            var age_f = jObject.GetValue("age");
            if (age_f == null || gender == null)
                return -1;
            int age;
            if (!Int32.TryParse(age_f.Value<string>(), out age))
                return -1;
            return GetPerson(age, gender.Value<string>());
        }

        int GetPerson(int age, string gender)
        {
            int pers;
            if      (age <= 10) pers = 0;
            else if (age <= 16) pers = 1;
            else if (age <= 30) pers = 2;
            else                pers = 3;
            if (gender == "F" || gender == "f") pers += 4;
            return pers;
        }
    }
}
