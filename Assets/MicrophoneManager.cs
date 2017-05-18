using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class MicrophoneManager : MonoBehaviour
{
	[Tooltip("A text area for the recognizer to display the recognized strings.")]
	public Text DictationDisplay;
	private Dictionary<string, string> supportedTranslationLanguages;
	private string target_lang = "FR";
	private string target_langName = "French";

	private DictationRecognizer dictationRecognizer;

	private AudioSource audioSource;
	private bool ttsEnabled = true;
	private TextToSpeechManager tts;
	private bool wolframEnabled = false;
	private string wolframAPIKey = "7U9RW8-ERWQ8PXR65";

	// Use this string to cache the text currently displayed in the text box.
	private string englishS = "English: Waiting for speech";
	private string target_langS = "French: ";

	void WriteOut(string hypothesis = "")
	{
		if (hypothesis.Length > 0)
		{
			DictationDisplay.text = englishS + " " + hypothesis + "..." + "\n" + target_langS;
		} else
		{
			DictationDisplay.text = englishS + "\n" + target_langS;
		}
	}

	void Start()
	{
		Debug.Log("start");
		audioSource = gameObject.GetComponent<AudioSource>();
		tts = gameObject.GetComponent<TextToSpeechManager>();
		LoadSupportedLanguages();
		//SetTargetLang("chinese simplified");
		//StartCoroutine(TranslateText("Waiting for speech"));
		//StartCoroutine(AskWolfram("what is the population of canada"));
		dictationRecognizer = new DictationRecognizer();

		dictationRecognizer.DictationHypothesis += (text) =>
			{
				Debug.LogFormat("Dictation hypothesis: {0}", text);
				WriteOut(text);
			};


		dictationRecognizer.DictationResult += (text, confidence) =>
			{
				Debug.LogFormat("Dictation result: {0}", text);
				var bits = text.Split(' ');
				if (bits[0] == "translate" && (bits.Length == 3 || bits.Length == 4))
				{

					string name = bits[2];
					if (text.EndsWith("simplified chinese") || text.EndsWith("chinese simplified"))
					{
						name = "chinese simplified";
					}
					else if (text.EndsWith("traditional chinese") || text.EndsWith("chinese traditional"))
					{
						name = "chinese traditional";
					}
					else if (text.EndsWith("marry") || text.EndsWith("mouldy") || text.EndsWith("moldy"))
					{
						name = "maori";
					}
					else if (text.EndsWith("finish"))
					{
						name = "finnish";
					}

					SetTargetLang(name);
				}
				else if (text == "mute" || text == "shut up" || text == "TTS off" || text == "turn off TTS" || text == "text to speech off" || text == "turn off text to speech")
				{
					ttsEnabled = false;
					englishS = "TTS off";
					WriteOut();
				}
				else if (text == "speak" || text == "talk now" || text == "TTS on" || text == "turn on TTS" || text == "text to speech on" || text == "turn on text to speech")
				{
					ttsEnabled = true;
					englishS = "TTS on";
					WriteOut();
				}
				else if (text == "answer my questions") {
					wolframEnabled = true;
					englishS = "Wolfram mode on";
					WriteOut();
				}
				else if (text == "translate for me")
				{
					wolframEnabled = false;
					englishS = "TTS on";
					WriteOut();
				}
				else
				{
					englishS = "english: " + text + ". ";
					WriteOut();
					if (wolframEnabled)
					{
						StartCoroutine(AskWolfram(text));
					}
					else
					{
						StartCoroutine(TranslateText(text));
					}
				}
			};

		dictationRecognizer.DictationComplete += (completionCause) =>
			{
				Debug.LogErrorFormat("Dictation completed: {0}.", completionCause);
				dictationRecognizer.Start();
			};

		dictationRecognizer.DictationError += (error, hresult) =>
			{
				Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
			};

		dictationRecognizer.Start();
	}

	private IEnumerator AskWolfram(string text)
	{
		englishS = "Question: " + text;
		target_langS = "Result: ";
		WriteOut();
		string url = string.Format("http://api.wolframalpha.com/v1/spoken?appid={0}&i={1}", wolframAPIKey, WWW.EscapeURL(text));
		var www = new WWW(url);
		yield return www;
		var result = www.text;
		Debug.Log(string.Format("Wolfram response to {0}:{1}", text, result));
		target_langS += result;
		WriteOut();
		if (ttsEnabled)
		{
			tts.SpeakText(result);
		}
	}

	void LoadSupportedLanguages()
	{
		var supportedTranslationLanguagesA = Resources.Load<TextAsset>("google_translate_supported_languages").text.Split('\n');
		supportedTranslationLanguages = new Dictionary<string, string>();
		foreach (var line in supportedTranslationLanguagesA)
		{
			var bits = line.Trim().Split();
			var code = bits[bits.Length - 1].Trim();
			var name = line.Replace(code, "").ToLower().Trim();
			//Debug.Log(name);
			supportedTranslationLanguages[name] = code;
		}
	}

	void SetTargetLang(string name)
	{
		name = name.ToLower();
		if (supportedTranslationLanguages.ContainsKey(name))
		{
			target_langName = name;
			target_lang = supportedTranslationLanguages[name];
			englishS = "Set target lang to " + target_lang;
			Debug.Log(englishS);
			target_langS = name + ": ";
		} else
		{
			englishS = name + " is not a supported language";
			Debug.Log(englishS);
			target_langS = name + ": ";
		}
		WriteOut();
	}

	IEnumerator<object> TranslateText(string text)
	{
		string url = string.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl=EN&tl={0}&dt=t&q={1}", target_lang, WWW.EscapeURL(text));
		Debug.Log(url);
		WWW www = new WWW(url);
		yield return www;
		JSONObject j = new JSONObject(www.text);
		string result = j[0][0][0].str;
		Debug.Log("Translation result: " + result);
		target_langS = target_langName + ": " + result;
		WriteOut();
		if (ttsEnabled)
		{
			url = string.Format("http://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&tl={0}&q={1}", target_lang, WWW.EscapeURL(result));
			Debug.Log(url);
			var headers = new Hashtable();
			headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.96 Safari/537.36");
			www = new WWW(url);
			yield return www;
			if (www.size > 0)
			{
				var audioClip = DecodeMP3.GetAudioClipFromMP3ByteArray(www.bytes);
				audioSource.PlayOneShot(audioClip);
			}
		}
	}

	void OnApplicationQuit()
	{
		Debug.Log("Application ending after " + Time.time + " seconds");
		dictationRecognizer.Stop();
		dictationRecognizer.Dispose();
	}

}