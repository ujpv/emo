#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using SpeechLib;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "TTS_Emo", Category = "Value", Help = "Basic template with one value in/out", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ValueTTS_EmoNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region input pins
		[Input("Male voices name", EnumName = "Voices", IsSingle = true)]
		public IDiffSpread<EnumEntry> InMaleVoiceName;

        [Input("Female voices name", EnumName = "Voices", IsSingle = true)]
        public IDiffSpread<EnumEntry> InFemaleVoiceName;

	    [Input("Picth", IsSingle = true, DefaultValue = 0)]
        public ISpread<int> InPith;

	    [Input("Gender (0: M, 1: F)", IsSingle = true, DefaultValue = 0)]
        public IDiffSpread<int> InGender;

        [Input("Text", IsSingle = true)]
        public IDiffSpread<string> InText;

        [Input("Go", DefaultValue = 0, IsSingle = false)]
        public IDiffSpread<bool> InGo;

        [Output("Done", IsSingle = true, DefaultBoolean = false)]
        public ISpread<bool> OutDone;

        [Output("Speking", IsSingle = true, DefaultBoolean = false)]
        public ISpread<bool> OutSpeaking;

        [Output("Visemes")]
        public ISpread<int> OutVisemes;

        [Output("Position")]
        public ISpread<int> OutPositions;

        [Output("Durations")]
        public ISpread<int> OutDuratios;

        [Output("State", IsSingle = true, DefaultBoolean = false)]
        public ISpread<string> OutState;
        #endregion


        #region Utis
        enum EnState
	    {
            READY,
            PREAPARING,
            READY_TO_SPEAK,
            SPEAKING,
            SPEAKING_FINISHED
	    }
        #endregion

        #region feailds
        [Import()]
        public ILogger FLogger;

	    private SpVoice FVox = new SpVoice();
        private SpVoice FVoxFake = new SpVoice();

	    private string FText = string.Empty;
        private List<int> FVisemes;
        private List<int> FPositions;
        private List<int> FDurations;
	    private int FTotlaDuration = 0;

        private bool FReloadVoice = false;
	    private EnState FState = EnState.READY;
        #endregion

        #region CONST
        const int BYTES_PER_SECOND = 32;
        readonly string[] visemesNames = {
                                            "silence",
                                            "ae ax ah",
                                            "aa",
                                            "ao",
                                            "ey eh uh",
                                            "er",
                                            "y iy ih ix",
                                            "w uw",
                                            "ow",
                                            "aw",
                                            "oy",
                                            "ay",
                                            "h",
                                            "r",
                                            "l", "s z",
                                            "sh ch jh zh",
                                            "th dh",
                                            "f v",
                                            "d t n",
                                            "k g ng",
                                            "p b m"
                                         };
        #endregion

        #region Constructor
        public void OnImportsSatisfied()
        {
            var voices = new SpVoice().GetVoices();
            string[] narrators = new string[voices.Count];
            for (int i = 0; i < voices.Count; ++i)
            {
                narrators[i] = voices.Item(i).GetDescription();
                FLogger.Log(LogType.Debug, $"[TTS_Emo] Voice #{i}: {narrators[i]}");
                EnumManager.AddEntry("Voices", voices.Item(i).GetDescription());
            }
            EnumManager.UpdateEnum("Voices", narrators[0], narrators);

            OutState.SliceCount = 1;
            OutState[0] = FState.ToString();
            OutDone[0] = true;
            OutSpeaking[0] = false;

            FVox.EndStream += FVox_EndStream;
            FVox.StartStream += FVox_StartStream;
            FVoxFake.EndStream += FVoxFake_EndStream;
            FVoxFake.Viseme += FVoxFake_Viseme;
        }


        #endregion

        public void Evaluate(int SpreadMax)
	    {
            if (InMaleVoiceName.IsChanged || InFemaleVoiceName.IsChanged || InGender.IsChanged)
            {
                FReloadVoice = true;
            }

	        switch (FState)
	        {
                case EnState.READY:
	                if (FReloadVoice)
	                {
                        SetupVoice();
	                }

	                if (InGo.IsChanged && InGo[0] && InText[0].Length > 0)
	                {
                        FState = EnState.PREAPARING;
                        OutState[0] = FState.ToString();

                        OutDone[0] = false;
                        FText = $"<pitch absmiddle=\"{InPith[0]}\"> {InText[0]} </pitch>";
                        FLogger.Log(LogType.Debug, $"[TTS_Emo] Text to speach: {FText}");
                        StartFake();
	                }
	                break;

                case EnState.READY_TO_SPEAK:
                    FState = EnState.SPEAKING;
                    OutState[0] = FState.ToString();

	                int count = FVisemes.Count;
	                OutVisemes.SliceCount = OutDuratios.SliceCount = OutPositions.SliceCount = count;
	                for (int i = 0; i < count; ++i)
	                {
	                    OutVisemes[i] = FVisemes[i];
	                    OutPositions[i] = FPositions[i];
	                    OutDuratios[i] = FDurations[i];
	                }
	                FVox.Speak(FText, SpeechVoiceSpeakFlags.SVSFlagsAsync | SpeechVoiceSpeakFlags.SVSFPurgeBeforeSpeak);
                    break;

                case EnState.SPEAKING_FINISHED:
                    FState = EnState.READY;
                    OutState[0] = FState.ToString();

                    OutDone[0] = true;
	                OutSpeaking[0] = false;
                    break;
                default:
                    break;
	        }
		}

        #region Utils methods
        private void SetupVoice()
        {
            FReloadVoice = false;

            int voiceNum = InGender[0] == 0 ? InMaleVoiceName[0].Index : InFemaleVoiceName[0].Index;
            FVox.Voice = FVox.GetVoices().Item(voiceNum);
            FVoxFake.Voice = FVox.GetVoices().Item(voiceNum);
        }

	    void StartFake()
	    {
            ISpeechBaseStream memoryStream = new SpMemoryStream();
            memoryStream.Format.Type = SpeechAudioFormatType.SAFT16kHz16BitMono;
	        FVoxFake.AudioOutputStream = memoryStream;
            FVisemes = new List<int>();
            FPositions = new List<int>();
            FDurations = new List<int>();
	        FTotlaDuration = 0;
 
            FVoxFake.Speak(FText, SpeechVoiceSpeakFlags.SVSFlagsAsync | SpeechVoiceSpeakFlags.SVSFPurgeBeforeSpeak);
	    }
        #endregion

        #region enevts
        private void FVoxFake_Viseme(int StreamNumber, 
            object StreamPosition, 
            int Duration, 
            SpeechVisemeType NextVisemeId, 
            SpeechVisemeFeature Feature, 
            SpeechVisemeType CurrentVisemeId)
        {
            int pos = Convert.ToInt32(StreamPosition);
            FVisemes.Add((int)CurrentVisemeId);
            FDurations.Add(Duration);
            FPositions.Add(pos / BYTES_PER_SECOND);
//            FLogger.Log(LogType.Debug, 
//                $"[TTS_Emo] Current vis: {CurrentVisemeId.ToString()}, Position: {pos}, Duration: {Duration}, Phoneme: {visemesNames[(int)CurrentVisemeId]}");
            FTotlaDuration += Duration;
        }

        private void FVoxFake_EndStream(int StreamNumber, object StreamPosition)
        {
            FLogger.Log(LogType.Debug, $"[TTS_Emo] Preparing finished. Position {StreamPosition}. Duration: {FTotlaDuration}.");
            FState = EnState.READY_TO_SPEAK;
            OutState[0] = FState.ToString();
        }

        private void FVox_EndStream(int streamNumber, object streamPosition)
        {
            FLogger.Log(LogType.Debug, $"[TTS_Emo] Speaking finished.");

            FState = EnState.SPEAKING_FINISHED;
            OutState[0] = FState.ToString();
        }

        private void FVox_StartStream(int StreamNumber, object StreamPosition)
        {
            FLogger.Log(LogType.Debug, $"[TTS_Emo] Speaking started.");
            OutSpeaking[0] = true;
        }
        #endregion
    }
}
