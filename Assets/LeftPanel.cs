using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeftPanel : MonoBehaviour
{
    public List<GameObject> Levels;

    public Button SwitchButton;

    public int currentLevel;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SwitchButton.onClick.AddListener(MarchForward);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MarchForward()
    {
        Levels[currentLevel].transform.localScale = Vector3.one;
        foreach(Transform e in Levels[currentLevel].transform)
            e.gameObject.SetActive(true);
        currentLevel++;
        if(currentLevel < Levels.Count)
            Levels[currentLevel].transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
    }

}
