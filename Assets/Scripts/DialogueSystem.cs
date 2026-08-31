using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class DialogueSystem : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text dialogueText;
    public GameObject dialoguePanel;
    public GameObject characterSprite;

    [Header("Dialogue Settings")]
    public string[] dialogueLines;
    public float typingSpeed = 0.05f;

    int currentLine = 0;
    bool isTyping = false;

    void Start()
    {
        if (PlayerPrefs.GetInt("IntroPlayed", 0) == 1)
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (characterSprite != null)
            {
                characterSprite.SetActive(false);
            }

            return;
        }
        StartDialogue();
    }

    void Update()
    {
        if (IsTapped())
        {
            HandleInput();
        }
    }

    bool IsTapped()
    {
        // Mobile touch
        if (Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
        }

        // PC / Mouse
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    public void StartDialogue()
    {
        currentLine = 0;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (characterSprite != null)
        {
            characterSprite.SetActive(true);
        }

        StartCoroutine(TypeLine());
    }

    void HandleInput()
    {
        if (dialoguePanel == null || !dialoguePanel.activeSelf)
        {
            return;
        }

        if (isTyping)
        {
            StopAllCoroutines();

            dialogueText.text = dialogueLines[currentLine];
            isTyping = false;
        }
        else
        {
            NextLine();
        }
    }

    void NextLine()
    {
        currentLine++;

        if (currentLine < dialogueLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            CloseDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in dialogueLines[currentLine])
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    void CloseDialogue()
    {
        StopAllCoroutines();

        isTyping = false;
        dialogueText.text = "";

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (characterSprite != null)
        {
            characterSprite.SetActive(false);
        }

        PlayerPrefs.SetInt("IntroPlayed", 1);
        PlayerPrefs.Save();

        Debug.Log("Dialogue Closed");
    }
}