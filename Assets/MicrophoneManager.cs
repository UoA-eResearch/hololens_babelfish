using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
		LoadSupportedLanguages();
		//SetTargetLang("chinese simplified");
		//StartCoroutine(TranslateText("Waiting for speech"));
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
					} else if (text.EndsWith("traditional chinese") || text.EndsWith("chinese traditional"))
					{
						name = "chinese traditional";
					}
					SetTargetLang(name);
					return;
				}
				englishS = "english: " + text + ". ";
				WriteOut();
				StartCoroutine(TranslateText(text));
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
	}

	void OnApplicationQuit()
	{
		Debug.Log("Application ending after " + Time.time + " seconds");
		dictationRecognizer.Stop();
		dictationRecognizer.Dispose();
	}

}