using KeepCoding;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    public ColourShuffleScript Parent;
    public MeshRenderer ColorDisplay;

    [SerializeField]
    private TextMesh ColorblindText;
    
    private float _baseChannel = 1 / 2f;
    private int _transitionFactor = 75;
    private int[] _components = new int[3];

    public void SetComponents(int[] component)
    {
        if (component.Length != _components.Length)
            throw new Exception("Mismatched component lengths.");

        for (int i = 0; i < _components.Length; i++)
            _components[i] = component[i];

        StartCoroutine(DisplayColor(_components));
    }

    public void SetBaseChannel(float baseChannel)
    {
        _baseChannel = baseChannel;
    }

    public void SetTransitionFactor(int newTransitionFactor)
    {
        _transitionFactor = newTransitionFactor;
    }

    private IEnumerator DisplayColor(int[] components)
    {
        Parent.IsAnimating = true;

        if (1 / 3f < _baseChannel && _baseChannel < 1f)
            ColorblindText.text = components.Join("").Replace("1", "½").Replace("2", "1");
        else if (1 / 5f < _baseChannel && _baseChannel < 1 / 3f)
            Parent.Selectables.Where(x => x.Index != 0).ForEach(x => x.ColorManager.ColorblindText.text = "");
        else if (!Parent.IsModuleSolved)
            ColorblindText.text = "KBGCRMYW"[(int)Enumerable.Range(0, components.Length).Select(x => components[x] * Math.Pow(2, components.Length - 1 - x)).Sum()].ToString();
        else
            Parent.Selectables.ForEach(x => x.ColorManager.ColorblindText.gameObject.SetActive(false));

        Color difference = (new Color(_baseChannel * components[0], _baseChannel * components[1], _baseChannel * components[2]) - ColorDisplay.material.color) / _transitionFactor;
        for(int i = 0; i < _transitionFactor; i++)
        {
            ColorDisplay.material.color += difference;
            yield return new WaitForSeconds(1 / _transitionFactor * 6);
        }
        Parent.IsAnimating = false;
    }
}