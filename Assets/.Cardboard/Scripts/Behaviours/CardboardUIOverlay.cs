using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MobfishCardboard
{
    public class CardboardUIOverlay: MonoBehaviour
    {
        public UnityEvent closeButtonEvent = new UnityEvent();
        private bool _showCloseButton = false;
        private bool _showSwitchVRButton = false;
        private bool _showScanQRCodeButton = true;

        // Whether to show the close button (cross icon) in the top-left of the screen when in VR mode
        public bool showCloseButton {
            get {
                return _showCloseButton;
            }
            set {
                _showCloseButton = value;
                SetUIStatus(CardboardManager.enableVRView);
            }
        }
        // Whether to show the switch to VR button (headset icon) in the bottom-right of the screen when not in VR mode
        public bool showSwitchVRButton {
            get {
                return _showSwitchVRButton;
            }
            set {
                _showSwitchVRButton = value;
                SetUIStatus(CardboardManager.enableVRView);
            }
        }
        // Whether to show the scan QR code button (QR icon) in the top-right of the screen when in VR mode
        public bool showScanQRCodeButton {
            get {
                return _showScanQRCodeButton;
            }
            set {
                _showScanQRCodeButton = value;
                SetUIStatus(CardboardManager.enableVRView);
            }
        }

        //Only used in dontDestroyAndSingleton
        public static CardboardUIOverlay instance {get; private set;}

        [Header("NoVRViewElements")]
        public Button switchVRButton;

        [Header("VRViewElements")]
        public Button scanQRButton;
        public Button closeButton;
        public GameObject splitLine;

        [Header("QROverlay")]
        public Text profileParamText;
        public Button continueButton;
        public GameObject continuePanel;

        [Header("Options")]
        [SerializeField]
        private bool dontDestroyAndSingleton;

        private bool overlayIsOpen;

        private void Awake()
        {
            if (dontDestroyAndSingleton)
            {
                if (instance == null)
                {
                    DontDestroyOnLoad(gameObject);
                    instance = this;
                }
                else if (instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            continueButton.onClick.AddListener(ContinueClicked);
            scanQRButton.onClick.AddListener(ScanQRCode);
            switchVRButton.onClick.AddListener(SwitchVRView);
            closeButton.onClick.AddListener(CloseVRView);
        }

        // Start is called before the first frame update
        void Start()
        {
            VRViewChanged();
            CardboardManager.deviceParamsChangeEvent += TriggerRefresh;
            CardboardManager.enableVRViewChangedEvent += VRViewChanged;
        }


        private void OnDestroy()
        {
            CardboardManager.deviceParamsChangeEvent -= TriggerRefresh;
            CardboardManager.enableVRViewChangedEvent -= VRViewChanged;
        }

        private void ScanQRCode()
        {
            CardboardManager.ScanQrCode();
            SetEnableQROverlay(true);
        }

        private void SwitchVRView()
        {
            CardboardManager.SetVRViewEnable(!CardboardManager.enableVRView);
        }

        private void CloseVRView()
        {
            // CardboardManager.SetVRViewEnable(false);
            closeButtonEvent.Invoke();
        }

        private void VRViewChanged()
        {
            SetEnableQROverlay(false);
            SetUIStatus(CardboardManager.enableVRView);
        }

        private void SetUIStatus(bool isVREnabled)
        {
            scanQRButton.gameObject.SetActive(isVREnabled && _showScanQRCodeButton);
            closeButton.gameObject.SetActive(isVREnabled && _showCloseButton);
            splitLine.SetActive(isVREnabled);

            switchVRButton.gameObject.SetActive(!isVREnabled && _showSwitchVRButton);

            if (isVREnabled)
                TriggerRefresh();
        }

        private void SetEnableQROverlay(bool shouldEnable)
        {
            continuePanel.SetActive(shouldEnable);
            overlayIsOpen = shouldEnable;
        }

        private void ContinueClicked()
        {
            TriggerRefresh();

            SetEnableQROverlay(false);
        }

        private void TriggerRefresh()
        {
            //CardboardManager.RefreshParameters();

            if (CardboardManager.enableVRView && !CardboardManager.profileAvailable)
            {
                SetEnableQROverlay(true);
            }

            if (CardboardManager.deviceParameter != null)
            {
                profileParamText.text =
                    CardboardManager.deviceParameter.Vendor + "\r\n" + CardboardManager.deviceParameter.Model;
            }
        }
    }
}