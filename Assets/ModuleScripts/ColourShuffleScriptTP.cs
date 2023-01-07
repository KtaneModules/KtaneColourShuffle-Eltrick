using KeepCoding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class ColourShuffleScriptTP : TPScript<ColourShuffleScript>
{
    public override IEnumerator ForceSolve()
    {
        yield return null;

        if (!Module.IsSubmission)
            Module.Selectables[Module.Rnd.Next(0, Module.ObjectCount)].Button.OnInteract();

        while(true)
        {
            while (Module.IsAnimating)
                yield return true;
            
            if (Module.IsModuleSolved)
                yield break;
            Module.Selectables.First(i => i.Index == Module.TargetVertex).Button.OnInteract();
        }
    }

    public override IEnumerator Process(string command)
    {
        yield return null;
        if (Regex.IsMatch(command, "(s|seq) ([01_]{3}\\s?)+", RegexOptions.IgnoreCase))
        {
            if(Module.IsSubmission)
            {
                yield return "sendtochaterror The module is in submission mode. There is no colour sequence to find. Please use indices.";
                yield break;
            }

            string input = Regex.Match(command, "([01_]{3}\\s?)+", RegexOptions.IgnoreCase).Value;
            string sequence = Module.StoredColours.Select(x => x.Join("")).Join(" ").Replace("1", "_").Replace("2", "1");
            sequence = sequence + " " + sequence;

            IEnumerable<int> matches = Enumerable.Range(0, Module.ObjectCount).Where(x => input == sequence.Substring(4 * x, input.Length));

            if (matches.Count() == 0)
                yield return "sendtochaterror The sequence is nowhere to be found in the current iteration. Please check your sequence.";
            else if (matches.Count() > 1)
                yield return "sendtochaterror The sequence occurs multiple times within the current iteration. Please give me more information to narrow down the vertex.";
            else
                Module.Selectables[matches.First()].Button.OnInteract();
        }
        else if (Regex.IsMatch(command, "(p|pos) [0-9]+"))
        {
            if(!Module.IsSubmission)
            {
                yield return "sendtochaterror The module is not in submission mode. Are you trying to softlock the module?";
                yield break;
            }

            int position;
            if (!int.TryParse(Regex.Match(command, "[0-9]+").Value, out position))
                yield return "sendtochaterror The 'position' requested is not an integer. Please check your command.";
            else if (position >= Module.ObjectCount)
                yield return "sendtochaterror The position requested is out of bounds on the module. Please check your command.";
            else
                Module.Selectables.First(i => i.Index == position).Button.OnInteract();
        }
        else if (Regex.IsMatch(command, "(b|bg|back|module|mod|m)"))
            if(!Module.IsSubmission)
                Module.Background.OnInteract();
        else
            yield return "sendtochaterror No valid commands detected. Try again.";
    }
}
