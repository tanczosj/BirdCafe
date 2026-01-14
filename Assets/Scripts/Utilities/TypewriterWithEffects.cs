using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TypewriterWithEffects : MonoBehaviour
{
    [Header("References")]
    public TMP_Text textMesh;

    [Header("Typewriter Settings")]
    public float typeDelay = 0.03f;

    private string finalText;
    private string visibleText;
    private List<TextEffect> charEffects = new List<TextEffect>();

    private enum EffectType { Default, Wavy, Shake }

    private struct TextEffect
    {
        public EffectType effect;
        public int charIndex; // index in TMP text
    }

    private bool typingFinished = false;

    void Start()
    {
        StartTyping();
    }

    public void StartTyping()
    {
        StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        ParseEffects();
        textMesh.text = visibleText = "";
        textMesh.ForceMeshUpdate();

        for (int i = 0; i < finalText.Length; i++)
        {
            // Skip invisible TMP formatting tags
            if (finalText[i] == '<')
            {
                int end = finalText.IndexOf('>', i);
                visibleText += finalText.Substring(i, end - i + 1);
                i = end;
            }
            else
            {
                visibleText += finalText[i];
            }

            textMesh.text = visibleText;
            textMesh.ForceMeshUpdate();

            yield return new WaitForSeconds(typeDelay);
        }

        typingFinished = true;
    }

    private void Update()
    {
        if (!typingFinished) return;

        textMesh.ForceMeshUpdate();
        TMP_TextInfo info = textMesh.textInfo;

        for (int i = 0; i < charEffects.Count; i++)
        {
            int index = charEffects[i].charIndex;
            if (index >= info.characterCount) continue;

            var charInfo = info.characterInfo[index];
            if (!charInfo.isVisible) continue;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] verts = info.meshInfo[meshIndex].vertices;

            switch (charEffects[i].effect)
            {
                case EffectType.Wavy:
                    float offset = Mathf.Sin(Time.time * 5f + index * 0.2f) * 5f;
                    ApplyOffset(ref verts, vertexIndex, new Vector3(0, offset, 0));
                    break;

                case EffectType.Shake:
                    Vector3 shake = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                    ApplyOffset(ref verts, vertexIndex, shake);
                    break;
            }
        }

        // Push mesh back
        for (int i = 0; i < info.meshInfo.Length; i++)
        {
            var meshInfo = info.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            textMesh.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private void ApplyOffset(ref Vector3[] verts, int index, Vector3 offset)
    {
        verts[index + 0] += offset;
        verts[index + 1] += offset;
        verts[index + 2] += offset;
        verts[index + 3] += offset;
    }

    private void ParseEffects()
    {
        string source = textMesh.text;

        finalText = "";
        charEffects.Clear();

        Stack<EffectType> effectStack = new Stack<EffectType>();
        effectStack.Push(EffectType.Default);

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == '<')
            {
                int end = source.IndexOf('>', i);
                string tag = source.Substring(i, end - i + 1);

                if (tag == "<wavy>") effectStack.Push(EffectType.Wavy);
                else if (tag == "</wavy>") effectStack.Pop();

                else if (tag == "<shake>") effectStack.Push(EffectType.Shake);
                else if (tag == "</shake>") effectStack.Pop();

                else
                {
                    finalText += tag; // normal TMP formatting tag
                }

                i = end;
            }
            else
            {
                finalText += source[i];

                charEffects.Add(new TextEffect
                {
                    effect = effectStack.Peek(),
                    charIndex = charEffects.Count
                });
            }
        }
    }
}
