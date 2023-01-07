using KeepCoding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rand = UnityEngine.Random;

public class ColourShuffleScript : ModuleScript
{
    internal KMBombModule BombModule;

    [SerializeField]
    internal KMSelectable Background;
    private KMSelectable[] _shapes;
    internal Selectable[] Selectables;

    internal System.Random Rnd;

    [SerializeField]
    private GameObject _referenceObject;
    private GameObject[] _objects;

    [SerializeField]
    internal AudioClip[] SoundEffects;

    private static Dictionary<string, string> _colours = new Dictionary<string, string>
    {
        { "K", "000" }, { "B", "001" }, { "G", "010" }, { "C", "011" }, { "R", "100" }, { "M", "101" }, { "Y", "110" }, { "W", "111" }
    };

    internal bool IsModuleSolved, IsSeedSet, IsCounter, IsAnimating, IsSubmission;
    internal float Speed = 1f;
    internal int Seed, TargetVertex, StageCount = 3, ObjectCount = 12, Base = 3;
    internal int[] Multiplier = new int[2], Offset = new int[2], SubmissionColours = new int[3];
    private int[][] _ring, _otherRing;
    internal int[][] StoredColours;

    // Use this for initialization
    void Start()
    {
        if (!IsSeedSet)
        {
            Seed = Rand.Range(int.MinValue, int.MaxValue);
            Log("The seed is: " + Seed.ToString());
            IsSeedSet = true;
        }

        Rnd = new System.Random(Seed);
        // SET SEED ABOVE IN CASE OF BUGS!!
        // _rnd = new System.Random(loggedSeed);
        BombModule = Get<KMBombModule>();

        IsCounter = Rnd.Next(0, 2) == 1;

        _shapes = new KMSelectable[ObjectCount];
        _objects = new GameObject[ObjectCount];
        Selectables = new Selectable[ObjectCount];

        StoredColours = CreateInefficientJaggedArray(ObjectCount, 3);
        _ring = CreateInefficientJaggedArray(ObjectCount, 3);
        _otherRing = CreateInefficientJaggedArray(ObjectCount, 3);

        do
        {
            IEnumerable<int> possibleMultipliers = Enumerable.Range(0, ObjectCount).Where(x => GreatestCommonDivisor(x, ObjectCount) == 1);
            Multiplier = Enumerable.Range(0, 2).Select(_ => possibleMultipliers.ToArray()[Rnd.Next(0, possibleMultipliers.Count())]).ToArray();
            Offset = Enumerable.Range(0, 2).Select(_ => Rnd.Next(0, ObjectCount)).ToArray();
        } while (IsAmbiguousPermutation(Multiplier[0], Offset[0], Multiplier[1], Offset[1], ObjectCount));


        Log("The functions are: " + Multiplier[0] + "x + " + Offset[0] + "; " + Multiplier[1] + "x + " + Offset[1]);

        BombModule.GetComponent<KMSelectable>().Children = new KMSelectable[ObjectCount + 1];

        do
        {
            for (int i = 0; i < _ring.Length; i++)
                for (int j = 0; j < _ring[i].Length; j++)
                    _ring[i][j] = Rnd.Next(0, 2);
        } while (IsAmbiguousArray(_ring));

        do
        {
            for (int i = 0; i < _otherRing.Length; i++)
                for (int j = 0; j < _otherRing[i].Length; j++)
                    _otherRing[i][j] = Rnd.Next(0, 2);
        } while (IsAmbiguousArray(_otherRing));

        for (int i = 0; i < StoredColours.Length; i++)
            for (int j = 0; j < StoredColours[i].Length; j++)
                StoredColours[i][j] = _ring[i][j] + _otherRing[i][j];

        float referenceAngle = _referenceObject.transform.rotation.y;
        for (int i = 0; i < ObjectCount; i++)
        {
            var x = i;
            _objects[x] = Instantiate(_referenceObject, BombModule.transform);
            _shapes[x] = _objects[x].GetComponentInChildren<KMSelectable>();
            Selectables[x] = _shapes[x].GetComponent<Selectable>();
            Selectables[x].SetIndex(x);
            BombModule.GetComponent<KMSelectable>().Children[x] = _shapes[x];

            Selectables[x].ColorManager.SetComponents(StoredColours[i]);
            _objects[x].transform.Rotate(new Vector3(0, x * (360f / ObjectCount) + referenceAngle, 0));
        }
        BombModule.GetComponent<KMSelectable>().Children[ObjectCount] = Background;
        _referenceObject.SetActive(false);
        BombModule.GetComponent<KMSelectable>().UpdateChildrenProperly();

        Background.Assign(onInteract: () => { BackgroundPress(); });

        Log("Internal sequence #1: " + _ring.Select(x => x.Join("")).Join("|"));
        Log("Internal sequence #2: " + _otherRing.Select(x => x.Join("")).Join("|"));
        Log("The colour quantities, in order KBGCRMYW, for each internal sequence is: " + Count(_ring).Join("|") + " ; " + Count(_otherRing).Join("|"));

        StartCoroutine(FunnyStuffs());
    }

    private IEnumerator FunnyStuffs()
    {
        while (!IsModuleSolved)
        {
            yield return null;
            for (int i = 0; i < ObjectCount; i++)
                _objects[i].transform.Rotate(new Vector3(0, 180f / ObjectCount * Time.deltaTime * Speed * (IsCounter ? -1 : 1) /* + (_isCounter ? -1 : 1) * i / 8f */ /* * (2 * _rnd.Next(0, 2) - 1) */, 0));
        }
    }

    private void BackgroundPress()
    {
        if (IsAnimating || IsSubmission)
            return;

        /*
        for (int i = 0; i < _storedColours.Length; i++)
            for (int j = 0; j < _storedColours[i].Length; j++)
                _storedColours[i][j] = _rnd.Next(0, _base);
        
        for (int i = 0; i < _objectCount; i++)
            _selectables[i].ColorManager.SetComponents(_storedColours[i]);
        */

        ButtonEffect(Background, .5f, SoundEffects[0]);
        TransformColours(Multiplier, Offset);
    }

    private void TransformColours(int[] multiplier, int[] offset)
    {
        _ring = Permute(_ring, multiplier[0], offset[0]);
        _otherRing = Permute(_otherRing, multiplier[1], offset[1]);

        for (int i = 0; i < StoredColours.Length; i++)
            for (int j = 0; j < StoredColours[i].Length; j++)
                StoredColours[i][j] = _ring[i][j] + _otherRing[i][j];

        for (int i = 0; i < StoredColours.Length; i++)
            Selectables[i].ColorManager.SetComponents(StoredColours[i]);
    }

    internal void StartSubmission()
    {
        if (IsAnimating || IsSubmission) return;

        SubmissionColours = Enumerable.Range(0, _colours.Keys.Count).ToArray().Shuffle().Take(StageCount).ToArray();
        PickColour(SubmissionColours[0]);

        IsSubmission = true;
    }

    internal void PickColour(int colourPicker)
    {
        TargetVertex = (Multiplier[0] * Count(_ring)[colourPicker] + Multiplier[1] * Count(_otherRing)[colourPicker] + Offset[0] + Offset[1]) % ObjectCount;
        Selectables.ForEach(x => x.ColorManager.SetBaseChannel(1f));
        Selectables.ForEach(x => { if (x.Index == 0) x.ColorManager.SetComponents(_colours.Values.ToArray()[colourPicker].ToCharArray().Select(y => y - '0').ToArray()); });
        Selectables.ForEach(x =>
        {
            if (x.Index != 0)
            {
                x.ColorManager.SetBaseChannel(1 / 4f);
                x.ColorManager.SetComponents(new int[] { 1, 1, 1 });
            }
        });
        Log("Stage #" + (4 - StageCount) + " picked " + _colours.Keys.ToArray()[colourPicker]);
        Log("Stage #" + (4 - StageCount) + " vertex calculation: (" + Multiplier[0] + " * " + Count(_ring)[colourPicker] + " + " + Multiplier[1] + " * " + Count(_otherRing)[colourPicker] + " + " + Offset[0] + " + " + Offset[1] + ") % " + ObjectCount + " = " + TargetVertex);
    }

    internal IEnumerator SolveModule()
    {
        IsModuleSolved = true;
        Log("Module solved!");
        BombModule.HandlePass();

        PlaySound(BombModule.transform, false, SoundEffects[4]);

        for (int i = 0; i < StoredColours.Length; i++)
        {
            Selectables[i].ColorManager.SetBaseChannel(1f);
            Selectables[i].ColorManager.SetComponents(new int[] { 0, 0, 0 });
        }

        for(int i = 0; i < 100; i++)
        {
            yield return new WaitForSeconds(.5f);
            Speed -= .015f;
        }
        Speed = 0f;
    }

    private static int[][] Permute(int[][] input, int multiplier, int offset)
    {
        int[][] result = CreateInefficientJaggedArray(input.Length, input[0].Length);

        for (int i = 0; i < input.Length; i++)
            for (int j = 0; j < input[i].Length; j++)
                result[(i * multiplier + offset) % input.Length][j] = input[i][j];

        return result;
    }

    private static int[] PermuteSingle(int[] input, int multiplier, int offset)
    {
        int[] result = new int[input.Length];

        for (int i = 0; i < input.Length; i++)
            result[(i * multiplier + offset) % input.Length] = input[i];

        return result;
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        if (b == 0)
            return a;
        else
            return GreatestCommonDivisor(b, a % b);
    }

    private static int[][] CreateInefficientJaggedArray(int height, int width)
    {
        int[][] result = new int[height][];
        for (int i = 0; i < result.Length; i++)
            result[i] = new int[width];
        return result;
    }

    private static bool IsAmbiguousArray(int[][] input)
    {
        int[] possibleMultipliers = Enumerable.Range(0, input.Length).Where(x => GreatestCommonDivisor(x, input.Length) == 1).ToArray();
        int[] possibleOffsets = Enumerable.Range(0, input.Length).ToArray();

        for (int i = 0; i < possibleMultipliers.Length; i++)
            for (int j = 0; j < possibleOffsets.Length; j++)
            {
                if (i + j == 0)
                    continue;
                if (IsJaggedArrayEqual(Permute(input, possibleMultipliers[i], possibleOffsets[j]), input))
                    return true;
            }

        return false;
    }

    private static bool IsAmbiguousPermutation(int a1, int b1, int a2, int b2, int modulus)
    {
        int[] currentState1 = Enumerable.Range(0, modulus).ToArray();
        int[] currentState2 = Enumerable.Range(0, modulus).ToArray();

        bool[,] encounteredPairs = new bool[modulus, modulus];

        for (int i = 0; i < modulus; i++)
        {
            for (int j = 0; j < modulus; j++)
                encounteredPairs[currentState1[j], currentState2[j]] = true;

            currentState1 = PermuteSingle(currentState1, a1, b1);
            currentState2 = PermuteSingle(currentState2, a2, b2);
        }

        for (int i = 0; i < modulus; i++)
            for (int j = 0; j < modulus; j++)
                if (!encounteredPairs[i, j])
                    return true;

        return false;
    }

    private static bool IsJaggedArrayEqual(int[][] a, int[][] b)
    {
        for (int i = 0; i < a.Length; i++)
            for (int j = 0; j < a[i].Length; j++)
                if (a[i][j] != b[i][j])
                    return false;
        return true;
    }

    private static int[] Count(int[][] input)
    {
        return _colours.Values.Select(x => input.Count(y => y.Join("") == x)).ToArray();
    }
}
