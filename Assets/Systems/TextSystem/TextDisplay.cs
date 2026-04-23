using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextDisplay : MonoBehaviour
{
    public TMPro.TextMeshProUGUI textDisplay;
    public SpriteRenderer portraitDisplay;
    public SpriteRenderer[] iconDisplays;

    public string sourceText;
    public string displayText;
    public int showTextCounter;

    public float currentRate = 0.1f;
    public Color currentColor = Color.white;
    public Sprite currentPortrait;
    public string currentFont;

    // Internal state used while typing
    private Stack<string> _openStyleTags = new Stack<string>();
    private bool _colorOpen;
    private string _currentColorHex;
    private AudioSource _audioSource;

    private List<TextAffector> _pendingAffectors = new List<TextAffector>();
    private int _nextAffectorIndex;

    private Coroutine _typingCoroutine;

    void Awake()
    {
        // Ensure we have an AudioSource for speech/sound affectors
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Begins displaying a segment with optional profile defaults.
    /// This will handle inline tags (e.g. &lt;c:FF0000&gt;, &lt;r:0.05&gt;, &lt;d:0.5&gt;, &lt;b&gt;, &lt;i&gt;, &lt;u&gt;, &lt;s&gt;, &lt;d&gt; reset)
    /// and TextAffectors attached to the TextSegment (applied when the visible character index reaches the affector's PositionInText).
    /// </summary>
    public void ShowSegment(TextSegment segment, TextProfile profile = null)
    {
        if (segment == null)
        {
            return;
        }

        // Initialize state from profile and segment
        sourceText = segment.textValue ?? string.Empty;
        displayText = string.Empty;
        showTextCounter = 0;

        // Profile defaults
        if (profile != null)
        {
            currentRate = Mathf.Max(0.001f, profile.textRate);
            currentFont = profile.textFont;
            if (profile.faceSprites != null && segment.portraitSprite >= 0 && segment.portraitSprite < profile.faceSprites.Length)
            {
                currentPortrait = profile.faceSprites[segment.portraitSprite];
            }
        }

        // Apply portrait immediately
        if (portraitDisplay != null)
        {
            portraitDisplay.sprite = currentPortrait;
        }

        // Prepare affectors list (sorted by PositionInText)
        _pendingAffectors.Clear();
        if (segment.textAffectors != null)
        {
            foreach (var a in segment.textAffectors)
            {
                if (a != null)
                    _pendingAffectors.Add(a);
            }

            _pendingAffectors.Sort((x, y) => x.PositionInText.CompareTo(y.PositionInText));
        }

        _nextAffectorIndex = 0;
        _openStyleTags.Clear();
        _colorOpen = false;
        _currentColorHex = null;

        // Stop previous coroutine if any and start typing
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _typingCoroutine = TypeTextCoroutine().Begin();
    }

    private IEnumerator TypeTextCoroutine()
    {
        if (textDisplay == null)
            yield break;

        textDisplay.text = string.Empty;
        displayText = string.Empty;
        showTextCounter = 0;

        int i = 0;
        while (i < sourceText.Length)
        {
            // If next character starts a tag
            if (sourceText[i] == '<')
            {
                int end = sourceText.IndexOf('>', i + 1);
                if (end != -1)
                {
                    string tag = sourceText.Substring(i + 1, end - i - 1);
                    float delayFromTag = ParseTag(tag); // ParseTag may insert rich-text tags into displayText and/or change currentRate etc.

                    // Immediately update TMP text with any inserted tags
                    textDisplay.text = displayText;

                    // If ParseTag requested a pause, wait
                    if (delayFromTag > 0f)
                    {
                        yield return new WaitForSeconds(delayFromTag);
                    }

                    i = end + 1;
                    continue;
                }
            }

            // Check and apply any affectors that trigger at this visible character index
            while (_nextAffectorIndex < _pendingAffectors.Count && _pendingAffectors[_nextAffectorIndex].PositionInText <= showTextCounter)
            {
                ApplyAffector(_pendingAffectors[_nextAffectorIndex]);
                _nextAffectorIndex++;
            }

            // Normal visible character - append to displayText and update TMP
            char c = sourceText[i];
            displayText += c;
            textDisplay.text = displayText;
            showTextCounter++;
            i++;

            // Wait for the currentRate between visible characters
            yield return new WaitForSeconds(currentRate);
        }

        // End of text - ensure any remaining affectors that target positions beyond last char are applied
        while (_nextAffectorIndex < _pendingAffectors.Count)
        {
            ApplyAffector(_pendingAffectors[_nextAffectorIndex]);
            _nextAffectorIndex++;
        }

        // Optionally close any remaining open tags so the TMP content is valid
        CloseAllOpenTags();
        textDisplay.text = displayText;
        _typingCoroutine = null;
    }

    /// <summary>
    /// Parses an inline tag and updates display state.
    /// Returns a delay in seconds if the tag requests a pause (&lt;d:seconds&gt;), otherwise 0.
    /// Supported inline tags:
    ///  - c:HEX      => changes color, e.g. &lt;c:FF0000&gt;
    ///  - r:SECONDS  => changes typing rate, e.g. &lt;r:0.05&gt;
    ///  - d:SECONDS  => inserts a pause of SECONDS before continuing
    ///  - b, i, u, s => toggle bold/italic/underline/strikethrough by inserting opening tags
    ///  - d          => reset / close all styles and colors
    ///  - h          => (reserved for highlight; implemented as simple color toggle to yellow)
    /// </summary>
    public float ParseTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return 0f;

        // Normalize
        string t = tag.Trim();

        // key:value style
        int colon = t.IndexOf(':');
        if (colon >= 0)
        {
            string key = t.Substring(0, colon).ToLowerInvariant();
            string value = t.Substring(colon + 1);

            switch (key)
            {
                case "c":
                    // Color change - expect hex like FFFFFF or RRGGBBAA
                    string hex = value.Trim().Replace("#", string.Empty);
                    if (hex.Length == 3)
                    {
                        // Expand shorthand rgb e.g. "fff" -> "ffffff"
                        hex = new string(hex[0], 2) + new string(hex[1], 2) + new string(hex[2], 2);
                    }

                    // Insert TMP color tag
                    CloseColor(); // close previous color if any (we'll push this new one)
                    _currentColorHex = hex;
                    _colorOpen = true;
                    displayText += $"<color=#{_currentColorHex}>";
                    // Update currentColor (best-effort)
                    if (ColorUtility.TryParseHtmlString("#" + _currentColorHex, out Color parsed))
                    {
                        currentColor = parsed;
                    }
                    return 0f;

                case "r":
                    // Rate change
                    if (float.TryParse(value, out float rate))
                    {
                        currentRate = Mathf.Max(0.001f, rate);
                    }
                    return 0f;

                case "d":
                    // Inline delay in seconds
                    if (float.TryParse(value, out float delay))
                    {
                        return Mathf.Max(0f, delay);
                    }
                    return 0f;

                case "p":
                    // Optional: insert portrait or icon by index - not implemented here
                    return 0f;

                default:
                    return 0f;
            }
        }
        else
        {
            // Single-letter tags or keywords
            string key = t.ToLowerInvariant();
            switch (key)
            {
                case "b":
                    displayText += "<b>";
                    _openStyleTags.Push("</b>");
                    return 0f;
                case "i":
                    displayText += "<i>";
                    _openStyleTags.Push("</i>");
                    return 0f;
                case "u":
                    displayText += "<u>";
                    _openStyleTags.Push("</u>");
                    return 0f;
                case "s":
                    displayText += "<s>";
                    _openStyleTags.Push("</s>");
                    return 0f;
                case "d":
                    // Default style: close all open style and color tags
                    CloseAllOpenTags();
                    return 0f;
                case "h":
                    // Highlight: simple yellow color
                    CloseColor();
                    _currentColorHex = "FFFF66";
                    _colorOpen = true;
                    displayText += $"<color=#{_currentColorHex}>";
                    if (ColorUtility.TryParseHtmlString("#" + _currentColorHex, out Color parsed))
                    {
                        currentColor = parsed;
                    }
                    return 0f;
                default:
                    // Unknown tag - ignore
                    return 0f;
            }
        }
    }

    private void CloseColor()
    {
        if (_colorOpen)
        {
            displayText += "</color>";
            _colorOpen = false;
            _currentColorHex = null;
        }
    }

    private void CloseAllOpenTags()
    {
        // Close style tags in LIFO order
        while (_openStyleTags.Count > 0)
        {
            string closing = _openStyleTags.Pop();
            displayText += closing;
        }

        // Close color tag if any
        CloseColor();
    }

    private void ApplyAffector(TextAffector aff)
    {
        if (aff == null)
            return;

        // Handle specific affector subclasses by type
        if (aff is TextAffector.Color colorAff)
        {
            // Insert color change into display immediately
            string hex = ColorUtility.ToHtmlStringRGB(colorAff.color);
            // Close previous color and open new one
            CloseColor();
            _currentColorHex = hex;
            _colorOpen = true;
            displayText += $"<color=#{_currentColorHex}>";
            currentColor = colorAff.color;
            if (textDisplay != null)
                textDisplay.text = displayText;
        }
        else if (aff is TextAffector.Rate rateAff)
        {
            currentRate = Mathf.Max(0.001f, rateAff.rate);
        }
        else if (aff is TextAffector.Delay delayAff)
        {
            // Insert a short delay right away in the typing coroutine by injecting a "<d:...>" token.
            // We cannot yield here; instead, we append an inline delay marker into the source by performing a small synchronous pause:
            // To keep behavior consistent with ParseTag, we immediately perform a coroutine to wait.
            if (delayAff.delayTime > 0f)
            {
                // Start a temporary coroutine to pause further typing - easiest is to stop and restart the typing coroutine.
                if (_typingCoroutine != null)
                {
                    StopCoroutine(_typingCoroutine);
                }

                StartCoroutine(DelayThenResume(delayAff.delayTime));
            }
        }
        else if (aff is TextAffector.Speech speechAff)
        {
            if (speechAff.soundClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(speechAff.soundClip);
            }
        }
        else if (aff is TextAffector.Sound soundAff)
        {
            if (soundAff.soundClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(soundAff.soundClip);
            }
        }
        else if (aff is TextAffector.Icon iconAff)
        {
            if (iconAff.iconSprite != null && iconDisplays != null && iconDisplays.Length > 0)
            {
                // Put icon into the first display by default; projects can change behavior later
                iconDisplays[0].sprite = iconAff.iconSprite;
            }
        }
        else if (aff is TextAffector.Font fontAff)
        {
            // Placeholder: store font name - applying would require font assets
            currentFont = fontAff.font;
        }
        else if (aff is TextAffector.Style styleAff)
        {
            // Apply style toggles as immediate inline tags
            if (styleAff.bold)
            {
                displayText += "<b>";
                _openStyleTags.Push("</b>");
            }

            if (styleAff.italic)
            {
                displayText += "<i>";
                _openStyleTags.Push("</i>");
            }

            if (styleAff.underline)
            {
                displayText += "<u>";
                _openStyleTags.Push("</u>");
            }

            if (styleAff.strikethrough)
            {
                displayText += "<s>";
                _openStyleTags.Push("</s>");
            }

            if (textDisplay != null)
                textDisplay.text = displayText;
        }
    }

    private IEnumerator DelayThenResume(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        // Resume typing coroutine for remaining source text starting from current index.
        // We do this by starting a new TypeTextCoroutine that will continue from the current sourceText and showTextCounter.
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);
        _typingCoroutine = TypeTextCoroutine().Begin();
    }
}
