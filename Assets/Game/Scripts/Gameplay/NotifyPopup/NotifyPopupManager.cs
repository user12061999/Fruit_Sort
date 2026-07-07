using HAVIGAME;
using HAVIGAME.UI;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NotifyPopupManager : Singleton<NotifyPopupManager>
{
    [SerializeField] private NotifyPopup notifyPopup;

    private void Start()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void OnEnable()
    {
        Assign();
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        Assign();
    }

    public void Assign()
    {
        Canvas canvas = GetComponent<Canvas>();

        if (canvas)
        {
            canvas.worldCamera = Camera.main;
        }
    }
    public void PushNotify(string message)
    {
        NotifyPopup popup = notifyPopup.Spawn();

        popup.transform.SetParent(transform);
        popup.transform.localPosition = Vector3.zero;
        popup.transform.localRotation = Quaternion.identity;
        popup.transform.localScale = Vector3.one;

        popup.Show(message);
    }
}
