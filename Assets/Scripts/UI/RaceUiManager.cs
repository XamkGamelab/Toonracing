using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RaceUIManager : MonoBehaviour
    {
        [Header("Gui Elements")]
        [SerializeField] private TMPro.TextMeshProUGUI startCountdownText;
        [SerializeField] private GameObject quitVerifyPanel;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button cancelButton;
        
        public static RaceUIManager Instance;
        
        // Text fade and scale animation variables
        private float fadeSpeed = 2f;
        private Vector3 scaleMax = new Vector3(5f, 5f, 5f);
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            if(Instance)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            if (startCountdownText)
            {
                startCountdownText.text = "";
                startCountdownText.alpha = 0f;
                startCountdownText.transform.localScale = Vector3.one;
            }
            if(quitButton)
                quitButton.onClick.AddListener(OnQuitButtonClicked);
            if(cancelButton)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            if(quitVerifyPanel)
                quitVerifyPanel.SetActive(false);
        }

        // Update is called once per frame
        private void Update()
        {
            if(startCountdownText)
            {
                startCountdownText.alpha = Mathf.Lerp(startCountdownText.alpha, 0, Time.deltaTime * fadeSpeed);
                startCountdownText.transform.localScale = Vector3.Lerp(startCountdownText.transform.localScale, scaleMax, Time.deltaTime * fadeSpeed);
            }
        }
    
        public void UpdateStartCountdown(int countdown)
        {
            if(!startCountdownText)
                return;
            startCountdownText.text = countdown > 0 ? countdown.ToString() : "GO!";
            startCountdownText.alpha = 1f;
            startCountdownText.transform.localScale = Vector3.one;

        }
        
        private void OnCancelButtonClicked()
        {
            ShowQuitVerifyPanel(false);
        }
        
        public void ShowQuitVerifyPanel(bool show)
        {
            if(quitVerifyPanel)
                quitVerifyPanel.SetActive(show);
        }
        private void OnQuitButtonClicked()
        {
            // Handle quit button click
            Debug.Log("Quit button clicked. Returning to main menu...");
            // Load the main menu scene or perform any other necessary actions
            //UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
    }
}
