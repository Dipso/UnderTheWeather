using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using UnityEngine.UI;


/// <summary>
/// Under the weather.
/// </summary>
public class UnderTheWeather : MonoBehaviour {

	// Const/Static
	//-------------
	private const float updateDateIntervals = 60.0f;		// Intervals at which to update the date


	// Enums
	//------
	// App states
	public enum states
	{
		init = 0,
		gettingLocation,
		doneLocation,
		gettingWeather,
		gettingCountryName,
		idle,
		error,
	}


	// Public
	//-------
	// Default latitude and longitude (e.g. Cape Town latitude is -33.92584, and the longitude is 18.42322)
	public float defaultLatitude = -33.92584f;
	public float defaultLongitude = 18.42322f;

	[Space(10)]
	[Tooltip("Loading icon rotation speed.")]
	public float loadingIconSpeed = 100.0f;

	[Space(10)]
	public Sprite[] weatherIconSprites;

	[Header("Elements")]
	public GameObject centreHolder;
	public Text date;
	public Text description;
	public Text temperatureMax;
	public Text temperatureMin;
	public Text location;
	public Text errorMsg;
	public Image icon;
	public Image loadingIcon;




	// Private
	//--------
	private states state = states.init;				// App state
	private float latitude;							// Detected latitude
	private float longitude;						// Detected longitude

	private float updateDateDelay;					// Delay for updating the date

	private CountryNames countryNames;				// Helper for getting the country name
	private string countryCode;						// Country 2-char code
	private string cityName;						// City name

	private bool didSetIcon;						// Did set weather icon?



	// Methods
	//--------

	/// <summary>
	/// Use this for initialization.
	/// </summary>
	void Start()
	{
		latitude = defaultLatitude;
		longitude = defaultLongitude;

		countryNames = gameObject.GetComponent<CountryNames>();

		UpdateControls(true);

		// First get the user's location
		SetState(states.gettingLocation);

	}


	/// <summary>
	/// Sets the state.
	/// </summary>
	/// <param name="newState">New state.</param>
	void SetState(states newState, string error = null)
	{
		Debug.Log("SetState: " + newState);


		// Process old state
		if (state == states.gettingLocation)
		{
			if (Input.location.status == LocationServiceStatus.Running)
			{
				Input.location.Stop();
			}
		}


		// Process new state
		state = newState;

		switch (state)
		{
		case states.gettingLocation:
			{
				StartCoroutine(StartGetLocation());
				break;
			}
		case states.gettingWeather:
			{
				didSetIcon = false;
				StartCoroutine(StartGetWeather());
				break;
			}
		case states.gettingCountryName:
			{
				if ((countryNames != null) && (string.IsNullOrEmpty(countryCode) == false))
				{
					countryNames.GetCountryName(countryCode, GotCountryNameSuccess, GotCountryNameFail);
				}
				else
				{
					SetState(states.idle);
				}
				break;
			}
		case states.error:
			{
				if ((errorMsg != null) && (error != null))
				{
					errorMsg.text = error;
				}

				break;
			}
		} //switch


		UpdateControls();
	}


	/// <summary>
	/// Formats the temperature string.
	/// </summary>
	/// <returns>The temperature.</returns>
	/// <param name="temperature">Temperature.</param>
	string FormatTemperature(string temperature)
	{
		float temp;
		if (float.TryParse(temperature, out temp))
		{
			// Remove decimal places
			return (temp.ToString("f0") + "° C");
		}
		else
		{
			return (temperature + "° C");
		}
	}


	/// <summary>
	/// Gots the country name success.
	/// </summary>
	/// <param name="countryName">Country name.</param>
	void GotCountryNameSuccess(string countryName)
	{
		if ((location != null) && (string.IsNullOrEmpty(countryName) == false))
		{
			if (location != null)
			{
				location.text = cityName + ", " + countryName;
			}
		}

		SetState(states.idle);
	}


	/// <summary>
	/// Gots the country name fail.
	/// </summary>
	/// <param name="countryName">Country name.</param>
	void GotCountryNameFail(string countryName)
	{
		SetState(states.idle);
	}


	/// <summary>
	/// Set the icon's image (i.e. which weather icon to display)
	/// </summary>
	/// <param name="spriteName">Sprite name.</param>
	void SetIcon(string spriteName)
	{
		if ((icon == null) || (weatherIconSprites == null) || (weatherIconSprites.Length <= 0))
		{
			return;
		}

		int i;
		Sprite sprite;

		for (i = 0; i < weatherIconSprites.Length; i++)
		{
			sprite = weatherIconSprites[i];
			if ((sprite != null) && (sprite.name == spriteName))
			{
				didSetIcon = true;
				icon.sprite = sprite;
				icon.gameObject.SetActive(true);
				break;
			}
		}

	}


	/// <summary>
	/// Starts the get location.
	/// </summary>
	/// <returns>The get location.</returns>
	IEnumerator StartGetLocation()
	{
		// First, check if user has location service enabled
		if (!Input.location.isEnabledByUser)
		{
			Debug.Log("Location not enabled by user.");

			SetState(states.error, "Location not enabled by user.");

			#if UNITY_EDITOR
			// Editor: Get the weather data for testing in the editor, we'll use the default location
			SetState(states.gettingWeather);
			#endif

			yield break;
		}

		// Start service before querying location
		Input.location.Start();

		// Wait until service initializes
		int maxWait = 20;
		while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
		{
			yield return new WaitForSeconds(1);
			maxWait--;
		}

		// Service didn't initialize in 20 seconds
		if (maxWait < 1)
		{
			Debug.Log("Location initialisation timed out.");

			SetState(states.error, "Location initialisation timed out.");

			yield break;
		}

		// Connection has failed
		if (Input.location.status == LocationServiceStatus.Failed)
		{
			Debug.Log("Unable to determine device location.");

			SetState(states.error, "Unable to determine device location.");

			yield break;
		}
		else
		{
			// Access granted and location value could be retrieved
			Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);

			latitude = Input.location.lastData.latitude;
			longitude = Input.location.lastData.longitude;
		}

		// Stop service if there is no need to query location updates continuously
		Input.location.Stop();

		SetState(states.doneLocation);
	}


	/// <summary>
	/// Starts the get weather.
	/// </summary>
	/// <returns>The get weather.</returns>
	IEnumerator StartGetWeather()
	{
		string url = string.Format("http://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&type=accurate&mode=xml&units=metric&lang=en&appid=ecf9a5ac6225dc979690064f9b8c10cb",
									latitude, longitude);
		WWW www = new WWW(url);

		yield return www;

		if (www.error == null)
		{
			Debug.Log("Loaded following XML " + www.text);

			XmlDocument xmlDoc = new XmlDocument();
			XmlNode node;

			try
			{
				xmlDoc.LoadXml(www.text);


				node = GetNodeProperty(xmlDoc, "city", "name");
				if (node != null)
				{
					Debug.Log("City: " + node.InnerText);

					cityName = node.InnerText;
					if (location != null)
					{
						location.text = node.InnerText;
					}
				}

				if (countryNames != null)
				{
					node = GetNodeProperty(xmlDoc, "country", null);
					if (node != null)
					{
						Debug.Log("Country code: " + node.InnerText);

						countryCode = node.InnerText;
					}
				}


				node = GetNodeProperty(xmlDoc, "temperature", "min");
				if (node != null)
				{
					Debug.Log("Temperature Min: " + node.InnerText);

					if (temperatureMin != null)
					{
						temperatureMin.text = "min " + FormatTemperature(node.InnerText);
					}
				}

				node = GetNodeProperty(xmlDoc, "temperature", "max");
				if (node != null)
				{
					Debug.Log("Temperature Max: " + node.InnerText);

					if (temperatureMax != null)
					{
						temperatureMax.text = "max " + FormatTemperature(node.InnerText);
					}
				}


				node = GetNodeProperty(xmlDoc, "weather", "value");
				if (node != null)
				{
					Debug.Log("Description: " + node.InnerText);

					if (description != null)
					{
						description.text = FirstLetterToUpper(node.InnerText);
					}
				}

				node = GetNodeProperty(xmlDoc, "weather", "icon");
				if (node != null)
				{
					Debug.Log("Icon: " + node.InnerText);

					SetIcon(node.InnerText);
				}


				if ((countryNames != null) && (string.IsNullOrEmpty(countryCode) == false))
				{
					SetState(states.gettingCountryName);
				}
				else
				{
					SetState(states.idle);
				}

			}
			catch (System.Exception e)
			{
				Debug.Log("Parse XML error: " + e);

				SetState(states.error, "Weather data parse error.");
			}

		}
		else
		{
			Debug.Log("ERROR: " + www.error);

			SetState(states.error, www.error);
		}
			
	}


	/// <summary>
	/// Get the specified node's property, or its child node.
	/// </summary>
	/// <returns>The node.</returns>
	/// <param name="xmlDoc">Xml document.</param>
	/// <param name="nodeName">Node name.</param>
	/// <param name="propertyName">Property name.</param>
	XmlNode GetNodeProperty(XmlDocument xmlDoc, string nodeName, string propertyName)
	{
		XmlNodeList nodelist = xmlDoc.SelectNodes("//" + nodeName);
		XmlNode node, propertyNode;
		int i;

		if ((nodelist != null) && (nodelist.Count > 0))
		{
			for (i = 0; i < nodelist.Count; i++)
			{
				node = nodelist[i];
				if (node != null)
				{
					// Looking for property node?
					if (string.IsNullOrEmpty(propertyName) == false)
					{
						propertyNode = node.SelectSingleNode("@" + propertyName);
						return (propertyNode);
					}
					else
					{
						return (node);
					}
				}
			
				// We only want the first one
				break;
			}
		}

		return (null);
	}


	/// <summary>
	/// Update is called once per frame.
	/// </summary>
	void Update()
	{
		float dt = Time.deltaTime;

		UpdateState();

		UpdateAnimations(dt);

		// Update controls every so often
		updateDateDelay -= dt;
		if (updateDateDelay <= 0.0f)
		{
			updateDateDelay = updateDateIntervals;
			UpdateControls();
		}


		#if UNITY_ANDROID
		// Android: Back button closes the app
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
		#endif

	}


	/// <summary>
	/// Updates the state.
	/// </summary>
	void UpdateState()
	{
		switch (state)
		{
		case states.doneLocation:
			{
				SetState(states.gettingWeather);
				break;
			}
		} //switch
	}


	/// <summary>
	/// Updates the animations.
	/// </summary>
	/// <param name="dt">Dt.</param>
	void UpdateAnimations(float dt)
	{
		// Rotate the loading icon
		if ((loadingIcon != null) && (loadingIcon.gameObject.activeInHierarchy))
		{
			loadingIcon.transform.Rotate(new Vector3(0.0f, 0.0f, loadingIconSpeed * dt));
		}
	}


	/// <summary>
	/// Updates the UI controls.
	/// </summary>
	void UpdateControls(bool firstTime = false)
	{
		bool visible;

		if (firstTime)
		{
			// Initialise some controls
			if (description != null)
			{
				description.text = "";
			}

			if (temperatureMax != null)
			{
				temperatureMax.text = "";
			}

			if (temperatureMin != null)
			{
				temperatureMin.text = "";
			}

			if (location != null)
			{
				location.text = "";
			}

			if (errorMsg != null)
			{
				errorMsg.text = "";
			}

			if (icon != null)
			{
				icon.gameObject.SetActive(false);
			}

			if (loadingIcon != null)
			{
				loadingIcon.gameObject.SetActive(true);
			}
		}
		else
		{
			// Show/hide the loading icon
			if ((state == states.gettingLocation) || (state == states.gettingWeather) || 
				(state == states.gettingCountryName) || (state == states.doneLocation))
			{
				visible = true;
			}
			else
			{
				visible = false;
			}

			if ((loadingIcon != null) && (loadingIcon.gameObject.activeInHierarchy != visible))
			{
				loadingIcon.gameObject.SetActive(visible);
			}


			// Weather icon is hidden while loading icon is visible
			visible = ((state == states.idle) && (didSetIcon));
			if ((icon != null) && (icon.gameObject.activeInHierarchy != visible))
			{
				icon.gameObject.SetActive(visible);
			}


			// The following elements are hidden while loading icon is visible (still busy trying to get country name)
			visible = (state == states.idle);
			if ((centreHolder != null) && (centreHolder.activeInHierarchy != visible))
			{
				centreHolder.SetActive(visible);
			}
		}


		if (date != null)
		{
			date.text = string.Format("Today, {0:d MMMM yyyy}", DateTime.Now);
		}
	}



	/// <summary>
	/// Makes the first letter uppercase.
	/// </summary>
	/// <returns>The letter to upper.</returns>
	/// <param name="str">String.</param>
	static public string FirstLetterToUpper(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return ("");
		}

		if (str.Length > 1)
		{
			return (char.ToUpper(str[0]) + str.Substring(1));
		}

		return (str.ToUpper());
	}
}
