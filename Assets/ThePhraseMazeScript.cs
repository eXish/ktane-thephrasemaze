using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;
using Newtonsoft.Json;
using UnityEngine.UI;

public class PhraseMazeOption
{
    public String symbol;
    public String txt;
    public Int32 down;
    public Int32 up;
}

public class PhraseMazeOptions
{
    public List<PhraseMazeOption> options;
}

public class ThePhraseMazeScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] switchSelectables;
    public TextAsset phraseData;
    public Text display;
    public Animator[] switchAnimators;

    private PhraseMazeOptions phraseDataParsed;
    private string[,] mazeWalls = new string[17, 5]
                        {
                            { "U L", "U", "D", "U", "U R" },
                            { "L D R", "L D", "U R D", "L R", "L R" },
                            { "L U", "U D", "U D", "R D", "L R" },
                            { "L D", "U R D", "L U", "U R", "L R" },
                            { "L U", "U R", "L R", "L R", "L R" },
                            { "L R", "L D", "D R", "L", "D R" },
                            { "L D", "U D", "U R D", "L D", "U R" },
                            { "L U", "U D", "U R", "L U", "D R" },
                            { "L", "U R", "L", "R D", "L U R" },
                            { "L R", "L R", "L R", "L U", "R D" },
                            { "L D R", "L R", "L D", "D", "U R" },
                            { "L U", "R D", "L U", "U R", "L D R" },
                            { "L D", "U D", "R", "L D", "U R" },
                            { "L U", "U R", "L D R", "L U", "R" },
                            { "L R", "L", "U R", "L D R", "L R" },
                            { "L R", "L R", "L D", "U R", "L R" },
                            { "L D R", "L D", "U R", "L D", "R D" }
                        };
    private string[,] mazeCells = new string[17, 5]
                       {
                            { "I", ">", "\\", "B", "U" },
                            { "‘", "=", "9", "Z", "?" },
                            { "Æ", "*", "F", "T", "3" },
                            { "Ø", "@", "¡", "]", "(" },
                            { "N", "±", ",", "Q", "&" },
                            { ":", "A", "C", "'", "┼" },
                            { "«", "_", "7", "/", "¿" },
                            { "}", "J", "S", "1", "W" },
                            { "Y", "%", "■", "\"", "H" },
                            { "¢", "O", "+", "#", "5" },
                            { "[", "”", "D", "^", "▓" },
                            { "8", "$", "┴", "{", "." },
                            { "M", "R", "-", "|", "æ" },
                            { "`", "L", "2", "’", "!" },
                            { "X", ")", "P", "E", "┐" },
                            { ";", "4", "V", "“", "~" },
                            { "G", "<", "K", "»", "6" }
                       };
    private string[] directions = { "Up", "Down", "Left", "Right" };
    private bool[] switchUp = new bool[4];
    private int currentCellRow, currentCellCol;
    private int goalCellRow = -1, goalCellCol;
    private bool activated;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in switchSelectables)
        {
            KMSelectable flipped = obj;
            flipped.OnInteract += delegate () { FlipSwitch(flipped); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += delegate () { StartCoroutine(OnActivate()); };
    }

    void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            switchUp[i] = UnityEngine.Random.Range(0, 2) == 0 ? true : false;
            switchAnimators[i].SetBool("IsUp", switchUp[i]);
        }
        Debug.LogFormat("[The Phrase Maze #{0}] Initial switch states going clockwise from the up switch: {1}, {2}, {3}, {4}", moduleId, switchUp[0] ? "up" : "down", switchUp[3] ? "up" : "down", switchUp[1] ? "up" : "down", switchUp[2] ? "up" : "down");
    }

    IEnumerator OnActivate()
    {
        phraseDataParsed = JsonConvert.DeserializeObject<PhraseMazeOptions>(phraseData.text);
        currentCellRow = UnityEngine.Random.Range(0, 17);
        currentCellCol = UnityEngine.Random.Range(0, 5);
        display.text = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().txt;
        Debug.LogFormat("[The Phrase Maze #{0}] Displayed phrase: “{1}”", moduleId, phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().txt);
        Transform bombT = transform.parent;
        List<string> phrases = new List<string>();
        if (bombT != null)
        {
            for (int i = 0; i < bombT.childCount; i++)
            {
                if (bombT.GetChild(i).gameObject.name == "CrazyTalkModule(Clone)")
                {
                    while (bombT.GetChild(i).Find("Model/Canvas/Text").GetComponent<Text>().text == "") yield return null;
                    phrases.Add(bombT.GetChild(i).Find("Model/Canvas/Text").GetComponent<Text>().text);
                }
            }
        }
        if (phrases.Count > 0)
        {
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (phrases.Contains(phraseDataParsed.options.Where(x => x.symbol == mazeCells[Mod(i + currentCellRow, 17), Mod(j + currentCellCol, 5)]).First().txt))
                    {
                        goalCellRow = Mod(i + currentCellRow, 17);
                        goalCellCol = Mod(j + currentCellCol, 5);
                        Debug.LogFormat("[The Phrase Maze #{0}] There is at least 1 Crazy Talk present on the bomb", moduleId);
                        goto escape;
                    }
                    if (Mod(j + currentCellCol, 5) == 4)
                        i++;
                    if (j == 4)
                        i--;
                }
            }
            escape:;
        }
        else if (bomb.GetModuleIDs().Contains("RegularCrazyTalkModule") || bomb.GetModuleIDs().Contains("krazyTalk") || bomb.GetModuleIDs().Contains("placeholderTalk"))
        {
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (bomb.GetSerialNumber().Contains(mazeCells[Mod(currentCellRow - i, 17), Mod(currentCellCol - j, 5)]))
                    {
                        goalCellRow = Mod(currentCellRow - i, 17);
                        goalCellCol = Mod(currentCellCol - j, 5);
                        Debug.LogFormat("[The Phrase Maze #{0}] There is a Regular Crazy Talk, Krazy Talk, or Placeholder Talk on the bomb", moduleId);
                        goto escape;
                    }
                    if (Mod(currentCellCol - j, 5) == 0)
                        i++;
                    if (j == 0)
                        i--;
                }
            }
            escape:;
        }
        else
        {
            int ct = 0;
            for (int i = 0; i < bomb.GetIndicators().Count(); i++)
            {
                string ind = bomb.GetIndicators().ToList()[i];
                for (int j = 0; j < ind.Length; j++)
                {
                    if ("CRAZY".Contains(ind[j]))
                        ct++;
                }
            }
            if (ct >= 3)
            {
                Debug.LogFormat("[The Phrase Maze #{0}] The indicators on the bomb contain 3 or more letters in CRAZY", moduleId);
                for (int j = 0; j < 5; j++)
                {
                    for (int i = 0; i < 17; i++)
                    {
                        if (bomb.GetSerialNumber().Contains(phraseDataParsed.options.Where(x => x.symbol == mazeCells[Mod(currentCellRow - i, 17), Mod(j + currentCellCol, 5)]).First().up.ToString()) && bomb.GetSerialNumber().Contains(phraseDataParsed.options.Where(x => x.symbol == mazeCells[Mod(currentCellRow - i, 17), Mod(j + currentCellCol, 5)]).First().down.ToString()))
                        {
                            goalCellRow = Mod(currentCellRow - i, 17);
                            goalCellCol = Mod(j + currentCellCol, 5);
                            goto escape;
                        }
                        if (Mod(currentCellRow - i, 17) == 0)
                            j++;
                        if (i == 0)
                            j--;
                    }
                }
                escape:;
                if (goalCellRow == -1)
                {
                    goalCellRow = 15;
                    goalCellCol = 2;
                }
            }
            else if ((bomb.GetModuleNames().Count - bomb.GetSolvableModuleNames().Count) > 0)
            {
                Debug.LogFormat("[The Phrase Maze #{0}] There is a needy module on the bomb", moduleId);
                goalCellRow = 6;
                goalCellCol = 4;
            }
            else
            {
                for (int j = 0; j < 5; j++)
                {
                    for (int i = 0; i < 17; i++)
                    {
                        string phrase = phraseDataParsed.options.Where(x => x.symbol == mazeCells[Mod(i + currentCellRow, 17), Mod(currentCellCol - j, 5)]).First().txt;
                        char[] serialLetters = bomb.GetSerialNumberLetters().ToArray();
                        for (int k = 0; k < serialLetters.Length; k++)
                        {
                            if (!phrase.Contains(serialLetters[k]))
                                break;
                            else if (k == serialLetters.Length - 1)
                            {
                                goalCellRow = Mod(i + currentCellRow, 17);
                                goalCellCol = Mod(currentCellCol - j, 5);
                                Debug.LogFormat("[The Phrase Maze #{0}] A phrase contains all letters in the serial number", moduleId);
                                goto escape;
                            }
                        }
                        if (Mod(i + currentCellRow, 17) == 16)
                            j++;
                        if (i == 16)
                            j--;
                    }
                }
                escape:;
                if (goalCellRow == -1)
                {
                    Debug.LogFormat("[The Phrase Maze #{0}] No phrases contain all letters in the serial number", moduleId);
                    goalCellRow = 3;
                    goalCellCol = 1;
                }
            }
        }
        Debug.LogFormat("[The Phrase Maze #{0}] Goal phrase: “{1}”", moduleId, phraseDataParsed.options.Where(x => x.symbol == mazeCells[goalCellRow, goalCellCol]).First().txt);
        activated = true;
        yield return null;
    }

    void FlipSwitch(KMSelectable flipped)
    {
        if (moduleSolved != true && activated != false)
        {
            int index = Array.IndexOf(switchSelectables, flipped);
            switchUp[index] = !switchUp[index];
            switchAnimators[index].SetBool("IsUp", switchUp[index]);
            audio.PlaySoundAtTransform("switch", flipped.transform);
            Debug.LogFormat("[The Phrase Maze #{0}] Flipped the switch in the {1} direction at {2}", moduleId, directions[index], bomb.GetFormattedTime());
            if (mazeWalls[currentCellRow, currentCellCol].Contains(directions[index][0].ToString()))
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[The Phrase Maze #{0}] Hit a wall going in this direction, strike", moduleId);
                return;
            }
            if ((switchUp[index] && !bomb.GetFormattedTime().Contains(phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString())) || (!switchUp[index] && !bomb.GetFormattedTime().Contains(phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString())))
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[The Phrase Maze #{0}] Switch flipped at invalid time, strike", moduleId);
            }
            switch (index)
            {
                case 0:
                    currentCellRow--;
                    if (currentCellRow < 0)
                        currentCellRow = 16;
                    break;
                case 1:
                    currentCellRow++;
                    if (currentCellRow > 16)
                        currentCellRow = 0;
                    break;
                case 2:
                    currentCellCol--;
                    break;
                default:
                    currentCellCol++;
                    break;
            }
            if (currentCellRow == goalCellRow && currentCellCol == goalCellCol)
            {
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
                Debug.LogFormat("[The Phrase Maze #{0}] Successfully navigated to goal phrase, module solved", moduleId);
            }
            else
                Debug.LogFormat("[The Phrase Maze #{0}] New displayed phrase: {1}", moduleId, phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().txt);
            display.text = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().txt;
        }
    }

    int Mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} flip <up/down/left/right> <#> [Flips the specified switch when any digit of the bomb's timer is '#'] | Commands can be chained, for example: !{0} flip up 3 r 5 left 8";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*flip\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a switch and a digit!";
            else if (parameters.Length == 2)
                yield return "sendtochaterror Please specify a digit!";
            else if (parameters.Length % 2 == 0)
                yield return "sendtochaterror A switch or digit is missing from the command!";
            else 
            {
                for (int i = 1; i < parameters.Length; i += 2)
                {
                    if (!parameters[i].ToLower().EqualsAny("u", "up", "l", "left", "r", "right", "d", "down"))
                    {
                        yield return "sendtochaterror!f The specified switch '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                    if (!parameters[i + 1].EqualsAny("0", "1", "2", "3", "4", "5", "6", "7", "8", "9"))
                    {
                        yield return "sendtochaterror!f The specified digit '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                }
                yield return null;
                for (int i = 1; i < parameters.Length; i += 2)
                {
                    while (!bomb.GetFormattedTime().Contains(parameters[i + 1])) yield return "trycancel";
                    if (parameters[i].ToLower().EqualsAny("u", "up"))
                        switchSelectables[0].OnInteract();
                    else if (parameters[i].ToLower().EqualsAny("d", "down"))
                        switchSelectables[1].OnInteract();
                    else if (parameters[i].ToLower().EqualsAny("l", "left"))
                        switchSelectables[2].OnInteract();
                    else
                        switchSelectables[3].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!activated) yield return true;
        if (currentCellCol == goalCellCol && currentCellRow == goalCellRow)
        {
            string desired = "";
            string walls = mazeWalls[currentCellRow, currentCellCol].Replace(" ", "");
            string paths = "";
            for (int i = 0; i < 4; i++)
            {
                if (!walls.Contains(directions[i][0]))
                    paths += directions[i][0];
            }
            int rando = UnityEngine.Random.Range(0, paths.Length);
            if (paths[rando] == 'U')
            {
                if (switchUp[0])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switchSelectables[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (switchUp[1])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switchSelectables[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else if (paths[rando] == 'L')
            {
                if (switchUp[2])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switchSelectables[2].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (switchUp[3])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switchSelectables[3].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else if (paths[rando] == 'D')
            {
                if (switchUp[1])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switchSelectables[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (switchUp[0])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switchSelectables[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else if (paths[rando] == 'R')
            {
                if (switchUp[3])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switchSelectables[3].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (switchUp[2])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switchSelectables[2].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            var q = new Queue<int[]>();
            var allMoves = new List<Movement>();
            var startPoint = new int[] { currentCellRow, currentCellCol };
            var target = new int[] { goalCellRow, goalCellCol };
            q.Enqueue(startPoint);
            while (q.Count > 0)
            {
                var next = q.Dequeue();
                if (next[0] == target[0] && next[1] == target[1])
                    goto readyToSubmit;
                string paths = mazeWalls[next[0], next[1]];
                var cell = paths.Replace(" ", "");
                var allDirections = "ULRD";
                var offsets = new int[,] { { -1, 0 }, { 0, -1 }, { 0, 1 }, { 1, 0 } };
                for (int i = 0; i < 4; i++)
                {
                    var check = new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1] };
                    if (!cell.Contains(allDirections[i]) && !allMoves.Any(x => x.start[0] == check[0] && x.start[1] == check[1]))
                    {
                        q.Enqueue(new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1] });
                        allMoves.Add(new Movement { start = next, end = new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1] }, direction = i });
                    }
                }
            }
            throw new InvalidOperationException("There is a bug in The Phrase Maze's TP autosolver.");
            readyToSubmit:
            KMSelectable[] switches = new KMSelectable[] { switchSelectables[0], switchSelectables[2], switchSelectables[3], switchSelectables[1] };
            int[] indexes = new int[] { 0, 2, 3, 1 };
            var target2 = new int[] { target[0], target[1] };
            var lastMove = allMoves.First(x => x.end[0] == target2[0] && x.end[1] == target2[1]);
            var relevantMoves = new List<Movement> { lastMove };
            while (lastMove.start != startPoint)
            {
                lastMove = allMoves.First(x => x.end[0] == lastMove.start[0] && x.end[1] == lastMove.start[1]);
                relevantMoves.Add(lastMove);
            }
            for (int i = 0; i < relevantMoves.Count; i++)
            {
                string desired = "";
                if (switchUp[indexes[relevantMoves[relevantMoves.Count - 1 - i].direction]])
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().down.ToString();
                else
                    desired = phraseDataParsed.options.Where(x => x.symbol == mazeCells[currentCellRow, currentCellCol]).First().up.ToString();
                while (!bomb.GetFormattedTime().Contains(desired)) yield return true;
                switches[relevantMoves[relevantMoves.Count - 1 - i].direction].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    class Movement
    {
        public int[] start;
        public int[] end;
        public int direction;
    }
}