using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class DecimationScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] keypad;
    public KMSelectable submit;
    public TextMesh[] digitDisplays;
    public TextMesh operatorDisplay;
    public TextMesh inputDisplay;
    public GameObject[] stageIndicators;
    public Material[] indicatorColors;
    public GameObject submitObject;
    public KMHighlightable submitHL;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    int inputBox = 0;
    int solution = 0;
    int stage = 0;
    int topNum;
    int botNum;

    void Awake () {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable key in keypad) 
        {
            key.OnInteract += delegate () { KeypadPress(key); return false; };
        }
        submit.OnInteract += delegate () { Submit(); return false; };

    }

    void Start ()
    {
        GenerateStage(0);
    }

    void GenerateStage(int stageNumber)
    {
        topNum = UnityEngine.Random.Range(0, 100);
        botNum = UnityEngine.Random.Range(0, 100);
        digitDisplays[0].text = Convert.ToString(topNum / 10);
        digitDisplays[1].text = Convert.ToString(topNum % 10);
        digitDisplays[2].text = Convert.ToString(botNum / 10);
        digitDisplays[3].text = Convert.ToString(botNum % 10);

        switch (stage)
        {
            case 0:
                operatorDisplay.text = "+";
                solution = LunarAdd(topNum, botNum);
                break;
            case 1:
                operatorDisplay.text = "*";
                botNum %= 10;
                digitDisplays[2].text = Convert.ToString(botNum / 10);
                solution = LunarMult(topNum, botNum);
                break;
            case 2:
                operatorDisplay.text = "*";
                solution = LunarMult(topNum, botNum);
                break;
        }
        Debug.LogFormat("[Decimation #{0}] STAGE {1}: The displayed numbers are {2} and {3}.", moduleId, stage + 1, topNum, botNum);
        Debug.LogFormat("[Decimation #{0}] The correct answer is {1}.", moduleId, solution);

    }

    int LunarAdd(int A, int B)
    {
        string AString = Convert.ToString(A);
        string BString = Convert.ToString(B);
        string CString = String.Empty;
        int maxLength = Math.Max(AString.Length, BString.Length);
        while (AString.Length < maxLength)
        {
            AString = "0" + AString;
        }
        while (BString.Length < maxLength)
        {
            BString = "0" + BString;
        }
        for (int i = 0; i < maxLength; i++)
        {
            CString += Convert.ToChar(Math.Max(AString[i], BString[i]));
        }

        return Convert.ToInt32(CString);
    }
    int LunarMult(int A, int B)
    {
        string AString = Convert.ToString(A);
        string BString = Convert.ToString(B);
        int maxLength = Math.Max(AString.Length, BString.Length);
        int[] stringsToAdd = new int[maxLength];
        while (AString.Length < maxLength)
        {
            AString = "0" + AString;
        }
        while (BString.Length < maxLength)
        {
            BString = "0" + BString;
        }
        AString = Reverse(AString);
        BString = Reverse(BString);
        Debug.Log(AString);
        Debug.Log(BString);

        string temp;
        for (int i = 0; i < maxLength; i++)
        {
            temp = string.Empty;
            for (int j = 0; j < maxLength; j++)
            {
               temp += Math.Min(int.Parse(AString[j].ToString()), int.Parse(BString[i].ToString()));
            }
            for (int j = 0; j < i; j++)
            {
                temp = "0" + temp;
            }
            temp = Reverse(temp);
            stringsToAdd[i] = int.Parse(temp);
        }
        if (stringsToAdd.Length == 1)
        {
            return stringsToAdd[0];
        }
        else return LunarAdd(stringsToAdd[0], stringsToAdd[1]);

    }
    string Reverse(string input)
    {
        return input.ToCharArray().Reverse().Join("");
    }
    void KeypadPress(KMSelectable key)
    {
        key.AddInteractionPunch(0.25f);
        Audio.PlaySoundAtTransform("button chirp", key.transform);
        if (!moduleSolved)
        {
            inputBox = (inputBox * 10 + Array.IndexOf(keypad, key)) % 10000;
            inputDisplay.text = inputBox.ToString();
        }
    }

    void Submit()
    {
        if (!moduleSolved)
        {
            submit.AddInteractionPunch(1f);
            if (solution == inputBox)
            {
                Debug.LogFormat("[Decimation #{0}] You submitted {1}, that was correct.", moduleId, inputBox);
                inputBox = 0;
                inputDisplay.text = string.Empty;

                stageIndicators[stage].GetComponent<MeshRenderer>().material = indicatorColors[1];
                Audio.PlaySoundAtTransform("stage pass", submit.transform);
                if (stage == 2)
                {
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    StartCoroutine(SolveAnim());
                }
                else
                {
                    stage++;
                    GenerateStage(stage);
                }

            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Decimation #{0}] You submitted {1}, that was incorrect; strike.", moduleId, inputBox);
            }
        }
    }
    IEnumerator SolveAnim()
    {
        foreach (TextMesh text in digitDisplays)
        {
            text.text = string.Empty;
        }
        operatorDisplay.text = string.Empty;
        Vector3 sphereScale = submit.transform.localScale;
        while (sphereScale.x > 0)
        {
            sphereScale = submit.transform.localScale;
            submit.transform.localScale = new Vector3(sphereScale.x - 0.07f, sphereScale.y - 0.07f, sphereScale.z - 0.07f);
            submitHL.transform.localScale = new Vector3(sphereScale.x - 0.06f, sphereScale.y - 0.06f, sphereScale.z - 0.06f);
            yield return null;
        }
        submitObject.SetActive(false);
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command)
    {
        string[] parameters = Command.Trim().ToUpperInvariant().Split(' ');
        string numToSubmit = string.Empty;
        if ((parameters.Length != 2) || (parameters[0] != "SUBMIT"))
        {
            yield return "sendtochaterror";
        }
        else
        {
            numToSubmit = parameters[1];
            if (numToSubmit.Any(x => !"0123456789".Contains(x)))
            {
                yield return "sendtochaterror";
            }
            else
            {
                yield return null;
                foreach (char letter in numToSubmit)
                {
                    keypad[int.Parse(letter.ToString())].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                submit.OnInteract();
            }
         }

      yield return null;
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        while (!moduleSolved)
        {
            string toSubmit = solution.ToString();
            foreach (char letter in toSubmit)
            {
                keypad[int.Parse(letter.ToString())].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            submit.OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield return true;
        }
      yield return null;
    }
}
