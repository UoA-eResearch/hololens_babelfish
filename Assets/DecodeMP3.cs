using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DecodeMP3 : MonoBehaviour
{

	public static AudioClip GetAudioClipFromMP3ByteArray(byte[] in_aMP3Data)
	{
		AudioClip l_oAudioClip = null;
		Stream l_oByteStream = new MemoryStream(in_aMP3Data);
		var l_oMP3Stream = new MP3Sharp.MP3Stream(l_oByteStream);

		//Get the converted stream data
		MemoryStream l_oConvertedAudioData = new MemoryStream();
		byte[] l_aBuffer = new byte[2048];
		int l_nBytesReturned = -1;
		int l_nTotalBytesReturned = 0;

		while (l_nBytesReturned != 0)
		{
			l_nBytesReturned = l_oMP3Stream.Read(l_aBuffer, 0, l_aBuffer.Length);
			l_oConvertedAudioData.Write(l_aBuffer, 0, l_nBytesReturned);
			l_nTotalBytesReturned += l_nBytesReturned;
		}

		Debug.Log("MP3 file has " + l_oMP3Stream.ChannelCount + " channels with a frequency of " + l_oMP3Stream.Frequency);

		byte[] l_aConvertedAudioData = l_oConvertedAudioData.ToArray();
		Debug.Log("Converted Data has " + l_aConvertedAudioData.Length + " bytes of data");

		//Convert the byte converted byte data into float form in the range of 0.0-1.0
		float[] l_aFloatArray = new float[l_aConvertedAudioData.Length / 2];

		for (int i = 0; i < l_aFloatArray.Length; i++)
		{
			if (BitConverter.IsLittleEndian)
			{
				//Evaluate earlier when pulling from server and/or local filesystem - not needed here
				//Array.Reverse( l_aConvertedAudioData, i * 2, 2 );
			}

			//Yikes, remember that it is SIGNED Int16, not unsigned (spent a bit of time before realizing I screwed this up...)
			l_aFloatArray[i] = (float)(BitConverter.ToInt16(l_aConvertedAudioData, i * 2) / 32768.0f);
		}

		//For some reason the MP3 header is readin as single channel despite it containing 2 channels of data (investigate later)
		l_oAudioClip = AudioClip.Create("MySound", l_aFloatArray.Length, 1, l_oMP3Stream.Frequency * 2, false);
		l_oAudioClip.SetData(l_aFloatArray, 0);

		return l_oAudioClip;
	}
}
