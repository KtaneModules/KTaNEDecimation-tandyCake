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
    public MeshRenderer[] stageIndicators;
    public Material indLit;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    int inputBox;
    int solution;
    int stage = 0;
    int topNum;
    int botNum;

    void Awake () {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable key in keypad) 
            key.OnInteract += delegate () { KeypadPress(key); return false; };
        submit.OnInteract += delegate () { Submit(); return false; };
    }

    void Start ()
    {
        GenerateStage();
    }

    void GenerateStage()
    {
        topNum = UnityEngine.Random.Range(0, 100);
        botNum = UnityEngine.Random.Range(0, 100);

        switch (stage)
        {
            case 0:
                operatorDisplay.text = "+";
                solution = LunarAdd(topNum, botNum);
                break;
            case 1:
                operatorDisplay.text = "*";
                botNum %= 10;
                solution = LunarMult(topNum, botNum);
                break;
            case 2:
                operatorDisplay.text = "*";
                solution = LunarMult(topNum, botNum);
                break;
        }
        digitDisplays[0].text = (topNum / 10).ToString();
        digitDisplays[1].text = (topNum % 10).ToString();
        digitDisplays[2].text = (botNum / 10).ToString();
        digitDisplays[3].text = (botNum % 10).ToString();
        Debug.LogFormat("[Decimation #{0}] STAGE {1}: The displayed numbers are {2} and {3}.", moduleId, stage + 1, topNum, botNum);
        Debug.LogFormat("[Decimation #{0}] The correct answer is {1}.", moduleId, solution);

    }

    int LunarAdd(int first, int second)
    {
        int maxLength = Math.Max(first.ToString().Length, second.ToString().Length);
        string A = first.ToString().PadLeft(maxLength, '0');
        string B = second.ToString().PadLeft(maxLength, '0');
        int result = 0;
        for (int i = 0; i < maxLength; i++)
        {
            result *= 10;
            result += Math.Max(A[i], B[i]) - '0';
        }
        return result;
    }
    int LunarMult(int first, int second)
    {
        int maxLength = Math.Max(first.ToString().Length, second.ToString().Length);
        string A = first.ToString().PadLeft(maxLength, '0');
        string B = second.ToString().PadLeft(maxLength, '0');
        int append = 0;
        Stack<int> adders = new Stack<int>();
        for (int i = A.Length - 1; i >= 0; i--)
        {
            string result = "";
            for (int j = B.Length - 1; j >= 0; j--)
            {
                result = Math.Min(A[j], B[i]) - '0' + result;
            }
            for (int k = 0; k < append; k++)
                result += '0';
            append++;
            adders.Push(int.Parse(result));
        }
        Debug.Log(adders.Join());
        while (adders.Count > 1)
            adders.Push(LunarAdd(adders.Pop(), adders.Pop()));
        return adders.First();
    }

    void KeypadPress(KMSelectable key)
    {
        key.AddInteractionPunch(0.25f);
        Audio.PlaySoundAtTransform("button chirp", key.transform);
        if (!moduleSolved)
        {
            inputBox *= 10;
            inputBox += Array.IndexOf(keypad, key) % 10000; // Appends the digit, the fancy way!
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
                stageIndicators[stage].material = indLit;
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
                    GenerateStage();
                }
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Decimation #{0}] You submitted {1}, that was incorrect; strike.", moduleId, inputBox);
                inputBox = 0;
                inputDisplay.text = string.Empty;
            }
        }
    }
    IEnumerator SolveAnim()
    {
        foreach (TextMesh text in digitDisplays)
            text.text = string.Empty;
        operatorDisplay.text = string.Empty;
        while (submit.transform.localScale.x - 0.1f > 0)
        {
            submit.transform.localScale -= 0.1f * Vector3.one;
            yield return null;
        }
        submit.gameObject.SetActive(false);
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} submit 123 to submit that number into the module. The module will automatically clear input.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string input)
    {
        string command = input.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parameters.Count != 2 || parameters.First() != "SUBMIT")
            yield break;
        if (parameters.Last().Any(x => !"0123456789".Contains(x)))
        {
            yield return "sendtochaterror Invalid number";
            yield break;
        }
        yield return null;
        while (inputBox != 0)
        {
            keypad[0].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        foreach (char digit in parameters.Last())
        {
            keypad[digit - '0'].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        while (!moduleSolved)
        {
            while (inputBox != 0)
            {
                keypad[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            foreach (char letter in solution.ToString())
            {
                keypad[letter - '0'].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            submit.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
      yield return null;
    }
}
