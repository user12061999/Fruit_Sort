using HAVIGAME.UI;
using UnityEngine;

public class LevelController : MonoBehaviour {
    
    [SerializeField] private LevelController parent;
    [SerializeField] private LevelController child;

    private LoadLevelOption option;
    public LoadLevelOption Option => option;
    public LevelController Parent => parent;
    public LevelController Child => child;

    public virtual void Initilaze(LoadLevelOption option, LevelController parent) {
        this.option = option;
        this.parent = parent;

        if(parent != null)
        {
            parent.child = this;
        }
    }

    public virtual void Initilaze()
    {
        
    }
    public virtual void StartLevel() {
        if(parent != null)
        {
            //parent.StartLevel();
            return;
        }

        OnStartLevel();
    }

    public virtual void WinLevel(bool isWinBySkip = false) {
        if (parent != null)
        {
            parent.WinLevel();
            return;
        }
    Debug.Log(gameObject.name+ " WinLevel");
        OnWinLevel();
    }

    public virtual void LoseLevel() {
        if (parent != null)
        {
            parent.LoseLevel();
            return;
        }

        OnLoseLevel();
    }

    public virtual void DestroyLevel() {
        OnDestroyLevel();   

        if (parent != null)
        {
            parent.DestroyLevel();
            return;
        }
    }

    protected virtual void OnStartLevel()
    {

    }

    protected virtual void OnWinLevel(bool isWinBySkip = false)
    {

    }

    protected virtual void OnLoseLevel()
    {

    }

    protected virtual void OnDestroyLevel()
    {

    }
}