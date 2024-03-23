using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class PopupMessage : MonoBehaviour
{
	public static PopupMessage instance;
	public GameObject buttonConfirm, buttonLeft, buttonRight, buttonBack;
	public GameObject confirmGaze, leftGaze, rightGaze;
	public delegate void Callback(bool playerConfirmed);
	public Animator canvasAnimator;
	private bool waitForConfirm, waitForYes, waitForNo;
	public TextMeshProUGUI messageText;
	private float targetTime;
	// button customization
	public ProjectButton confirmPButton, leftPButton, rightPButton;


	public enum ButtonMode
	{
		None = 0,
		Confirm = 1,
		YesNo = 2,
		ConfirmBack = 3,
		Timer = 4
	}
	void Awake()
	{
		instance = this;
	}
	public void Confirm()
	{
		waitForConfirm = false;
	}
	public void Yes()
	{
		waitForYes = false;
	}
	public void No()
	{
		waitForNo = false;
	}
	
	public void Show(string message, ButtonMode buttonMode, Callback callback, float waitTime, bool gazeButtons = false)
	{
		Show(message, buttonMode, callback, null, null, null, waitTime, gazeButtons);
	}

	public void Show(string message, ButtonMode buttonMode, Callback callback, string confirmText = null, string leftText = null, string rightText = null, float waitTime = 10f, bool gazeButtons = false)
	{
		if (confirmGaze != null && leftGaze != null && rightGaze != null) {
			confirmGaze.SetActive(gazeButtons);
			leftGaze.SetActive(gazeButtons);
			rightGaze.SetActive(gazeButtons);
		}

		targetTime = Time.realtimeSinceStartup + waitTime;
		Debug.Log("Showing message 1");
		canvasAnimator.SetBool("ShowMessage", true);
		gameObject.SetActive(true);
		buttonConfirm.SetActive(buttonMode == ButtonMode.Confirm || buttonMode == ButtonMode.ConfirmBack);
		if (buttonBack != null) {
			buttonBack.SetActive(buttonMode == ButtonMode.ConfirmBack);
		}
		buttonLeft.SetActive(buttonMode == ButtonMode.YesNo);
		buttonRight.SetActive(buttonMode == ButtonMode.YesNo);
		messageText.text = message;
		// button customization
		if (confirmPButton != null && leftPButton != null && rightPButton != null) {
			if (!string.IsNullOrEmpty(confirmText)) {
				confirmPButton.normalText = confirmPButton.highlightedText = confirmText;
			} else {
				confirmPButton.normalText = confirmPButton.highlightedText = "Confirm";
			}
			if (!string.IsNullOrEmpty(leftText)) {
				leftPButton.normalText = leftPButton.highlightedText = leftText;
			} else {
				leftPButton.normalText = leftPButton.highlightedText = "No";
			}
			if (!string.IsNullOrEmpty(rightText)) {
				rightPButton.normalText = rightPButton.highlightedText = rightText;
			} else {
				rightPButton.normalText = rightPButton.highlightedText = "Yes";
			}
		}

		StartCoroutine(ShowMessage(message, buttonMode, callback));
	}
	private IEnumerator ShowMessage(string message, ButtonMode buttonMode, Callback callback)
	{
		bool result = true;
		yield return null;
		Debug.Log("Showing message 2");
		if (buttonMode == ButtonMode.Confirm) {
			waitForConfirm = true;
			while (waitForConfirm) yield return null;
			result = true;
		} else if (buttonMode == ButtonMode.ConfirmBack) {
			waitForConfirm = waitForNo = true;
			while (waitForConfirm && waitForNo) yield return null;
			if (!waitForConfirm) result = true;
			if (!waitForNo) result = false;
		} else if (buttonMode == ButtonMode.YesNo) {
			waitForYes = waitForNo = true;
			while (waitForYes && waitForNo) yield return null;
			if (!waitForYes) result = true;
			if (!waitForNo) result = false;
		} else if (buttonMode == ButtonMode.Timer) {
			while (Time.realtimeSinceStartup < targetTime) yield return null;
			result = true;
		} else {
			while (true) yield return null;
		}
		waitForConfirm = waitForYes = waitForNo = false;
		canvasAnimator.SetBool("ShowMessage", false);
		Debug.Log("Finished message");
		callback?.Invoke(result);
	}
}
