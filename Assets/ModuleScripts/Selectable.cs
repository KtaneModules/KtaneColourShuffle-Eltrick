using KeepCoding;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    public ColourShuffleScript Parent;
    public ColourShuffleScriptTP TwitchPlaysScript;
    public KMSelectable Button { get; private set; }
    public ColorManager ColorManager { get; private set; }

    internal float Speed = 20f;
    internal int Index;

    public void SetIndex(int i)
    {
        Index = i;
        Activate();
    }

    private void Activate()
    {
        Button = GetComponent<KMSelectable>();
        Button.Assign(onInteract: () => { ButtonPress(); });
        ColorManager = Button.GetComponent<ColorManager>();
        StartCoroutine(FunnyThings());
    }

    private void ButtonPress()
    {
        if (Parent.IsModuleSolved)
            return;
        if (!Parent.IsSubmission)
        {
            Enumerable.Range(0, 2).ForEach(x => Parent.Offset[x] = ((Parent.Multiplier[x] - 1) * Index + Parent.Offset[x]) % Parent.ObjectCount);
            int shift = Parent.ObjectCount - Index;
            Parent.Selectables.ForEach(x => x.Index = (x.Index + shift) % Parent.ObjectCount);

            Parent.ButtonEffect(Button, .5f, Parent.SoundEffects[1]);
            Parent.StartSubmission();
            return;
        }
        else
        {
            Parent.ButtonEffect(Button, .5f, Parent.SoundEffects[2]);
            if (Index == Parent.TargetVertex)
            {
                Parent.Log("Pressed correct vertex");
                Parent.StageCount--;
                if (Parent.StageCount == 0)
                    StartCoroutine(Parent.SolveModule());
                else
                    Parent.PickColour(Parent.SubmissionColours[3 - Parent.StageCount]);
            }
            else
            {
                Parent.Log("Pressed vertex #" + Index + ", strike.");
                Parent.BombModule.HandleStrike();
                Parent.StageCount = 3;
                Parent.IsSubmission = false;
                Parent.PlaySound(Button.transform, false, Parent.SoundEffects[3]);
                for (int i = 0; i < Parent.StoredColours.Length; i++)
                {
                    Parent.Selectables[i].ColorManager.SetBaseChannel(1 / 2f);
                    Parent.Selectables[i].ColorManager.SetComponents(Parent.StoredColours[i]);
                }
            }
        }
    }

    private IEnumerator FunnyThings()
    {
        while(Speed > 0f)
        {
            yield return null;
            Button.transform.Rotate(new Vector3(Parent.Rnd.Next(0, 3) * Mathf.PI / 2f * Time.deltaTime * Speed, Parent.Rnd.Next(3, 6) * Mathf.PI / 4f * Time.deltaTime * Speed, Parent.Rnd.Next(6, 9) * Mathf.PI / 6f * Time.deltaTime * Speed));
        }
    }
}