using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DisplayFrameRate : MonoBehaviour
{
	public int fontSize = 15;
	public Color fontColor = Color.blue;
	public float updateRate = .2f;

	float deltaTime;
	float updateInterval;
	float fps;

	Text text;

	void OnEnable()
	{
		text = GetComponent<Text>();
		text.alignment = TextAnchor.UpperLeft;
		text.color = fontColor;
		text.fontSize = fontSize;
	}

	void Update()
	{
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

		updateInterval -= Time.deltaTime;
		if(updateInterval < 0)
		{
			//float msec = deltaTime * 1000.0f;
			fps = 1.0f / deltaTime;
			updateInterval = updateRate;

			//text.text = string.Format("{0:0} fps", fps);
			text.text = fps.ToString();
		}
	}
}