using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts;

public class TitleController : MonoBehaviour {
    public void OnStartOfflineButtonClicked()
    {
        GameControllerState.IsOnline = false;
        SceneManager.LoadScene("SampleScene");
    }

    public void OnStartOnlineButtonClicked()
    {
        GameControllerState.IsOnline = true;
        SceneManager.LoadScene("SampleScene");
    }
}
