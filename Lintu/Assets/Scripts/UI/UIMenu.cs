﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIMenu : MonoBehaviour
{
    public TextMeshProUGUI VersionText;
    public Sprite []UnderlineImage;

    public GameObject PreviousButtonSelected;
    GameObject[] SelectionVertex;
    bool TriggeredButtonPrevFrame;

    private void Awake()
    {
        SelectionVertex = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            SelectionVertex[i] = new GameObject();
            SelectionVertex[i].AddComponent<Image>();
            SelectionVertex[i].GetComponent<Image>().sprite = UnderlineImage[i];
            SelectionVertex[i].transform.SetParent(transform);
            SelectionVertex[i].name = "Corner" + i;
            SelectionVertex[i].transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
    }

    private void Start()
    {
        if(VersionText)
            VersionText.SetText("v" + Application.version);
        PreviousButtonSelected = EventSystem.current.firstSelectedGameObject;
        
        PlaceVertexes(PreviousButtonSelected);
    }

    private void Update()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem.currentSelectedGameObject != null)
        {
            if (GameManager.Instance.GameInput.UI.Submit.triggered || GameManager.Instance.GameInput.UI.Cancel.triggered)
            {
                AkSoundEngine.PostEvent("Cursor_Seleccion", gameObject);
                TriggeredButtonPrevFrame = true;
            }
            else if (eventSystem.currentSelectedGameObject != PreviousButtonSelected && !TriggeredButtonPrevFrame)
                AkSoundEngine.PostEvent("Cursor", gameObject);
            else
                TriggeredButtonPrevFrame = false;
        }

        if (!eventSystem.currentSelectedGameObject && GameManager.Instance.GameInput.UI.Navigate.triggered)
        {
            eventSystem.SetSelectedGameObject(PreviousButtonSelected);
            PreviousButtonSelected = null;
            ActivateCornerImages();
        }

        GameObject ActualButton = eventSystem.currentSelectedGameObject;

        if(ActualButton != null && PreviousButtonSelected != ActualButton)
        {
            PreviousButtonSelected = ActualButton;
            if (ActualButton.GetComponent<Button>())
            {
                PlaceVertexes(ActualButton);
                ActivateCornerImages();
            }
            else
            {
                DeactivateCornerImages();
            }
        }
        else if(ActualButton == null)
        {
            DeactivateCornerImages();
        }
    }

    void PlaceVertexes(GameObject destObj)
    {
        for (int i = 0; i < 4; i++)
        {
            SelectionVertex[i].transform.SetParent(destObj.transform);
            SelectionVertex[i].GetComponent<Image>().color = destObj.GetComponent<Image>().color;
            SelectionVertex[i].transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }

        SelectionVertex[0].transform.localPosition = new Vector2(destObj.GetComponent<RectTransform>().rect.xMin, destObj.GetComponent<RectTransform>().rect.yMax); //top-left
        SelectionVertex[1].transform.localPosition = new Vector2(destObj.GetComponent<RectTransform>().rect.xMax, destObj.GetComponent<RectTransform>().rect.yMax); //top-right
        SelectionVertex[2].transform.localPosition = new Vector2(destObj.GetComponent<RectTransform>().rect.xMin, destObj.GetComponent<RectTransform>().rect.yMin); //bottom-left
        SelectionVertex[3].transform.localPosition = new Vector2(destObj.GetComponent<RectTransform>().rect.xMax, destObj.GetComponent<RectTransform>().rect.yMin); //bottom-right
    }

    void ActivateCornerImages()
    {
        for (int i = 0; i < 4; i++)
        {
            SelectionVertex[i].SetActive(true);
        }
    }

    void DeactivateCornerImages()
    {
        for (int i = 0; i < 4; i++)
        {
            SelectionVertex[i].SetActive(false);
        }
    }

    private void OnEnable()
    {
        if(SelectionVertex!=null)
            ActivateCornerImages();
    }

    private void OnDisable()
    {
        DeactivateCornerImages();
    }
}
