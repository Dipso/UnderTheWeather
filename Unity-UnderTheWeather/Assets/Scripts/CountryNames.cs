using UnityEngine;
using System.Collections;
using SimpleJSON;

/// <summary>
/// Helper to get the country name, from the country code.
/// </summary>
public class CountryNames : MonoBehaviour {

	// Const/Static
	//-------------
	public delegate void ResultCallback(string countryName);			// Result callback


	// Private
	//--------
	private string code;
	private string countryName;
	private ResultCallback successCallback;
	private ResultCallback failCallback;
	private string jsonString;


	// Methods
	//--------

	/// <summary>
	/// Gets the name of the country.
	/// </summary>
	/// <param name="code">ISO 2 character country code.</param>
	/// <param name="successCallback">Success callback.</param>
	/// <param name="failCallback">Fail callback.</param>
	public void GetCountryName(string code, ResultCallback successCallback, ResultCallback failCallback)
	{
		if (string.IsNullOrEmpty(code) == false)
		{
			this.code = code.ToUpper();
		}
		else
		{
			this.code = null;
		}

		this.successCallback = successCallback;
		this.failCallback = failCallback;
		countryName = "";

		StartCoroutine(StartGetCountryName());
	}


	/// <summary>
	/// Fires the success callback.
	/// </summary>
	void FireSuccessCallback()
	{
		ResultCallback tempCallback = successCallback;
		successCallback = null;
		failCallback = null;
		if (tempCallback != null)
		{
			tempCallback(countryName);
		}
	}


	/// <summary>
	/// Fires the fail callback.
	/// </summary>
	void FireFailCallback()
	{
		ResultCallback tempCallback = failCallback;
		successCallback = null;
		failCallback = null;
		if (tempCallback != null)
		{
			tempCallback(null);
		}
	}


	/// <summary>
	/// Starts the name of the get country.
	/// </summary>
	/// <returns>The get country name.</returns>
	IEnumerator StartGetCountryName()
	{
		// Get json string that contains all country codes and names, from the website.
		string url = "http://country.io/names.json";
		WWW www = new WWW(url);

		yield return www;

		if (www.error == null)
		{
			try
			{
				jsonString = www.text;

				if (string.IsNullOrEmpty(jsonString) == false)
				{
					JSONNode node = JSON.Parse(jsonString);
					if ((node != null) && (string.IsNullOrEmpty(code) == false) && (node[code] != null))
					{
						countryName = node[code].Value;
						FireSuccessCallback();

						yield break;
					}
				}
			}
			catch
			{
			}
		}

		FireFailCallback();

		yield break;
	}

}
