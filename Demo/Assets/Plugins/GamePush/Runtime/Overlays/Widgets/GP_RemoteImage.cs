using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GamePush.Overlays.Widgets
{
    /// <summary>
    /// Loads avatars and icons off the network into an Image, with a process-wide texture cache
    /// so scrolling a list does not re-download the same picture.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class GP_RemoteImage : MonoBehaviour
    {
        const int CacheLimit = 128;
        static readonly Color EmptyFill = new Color(1f, 1f, 1f, 0.12f);

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly List<string> CacheOrder = new List<string>();

        public Sprite placeholder;

        Image _image;
        string _url;
        Coroutine _routine;

        Image Target => _image != null ? _image : _image = GetComponent<Image>();

        void Awake()
        {
            LockSquare();
        }

        void OnEnable()
        {
            LockSquare();
        }

        void LockSquare()
        {
            var layout = GetComponent<LayoutElement>();
            if (layout != null)
            {
                var size = Mathf.Max(layout.minWidth, layout.minHeight, layout.preferredWidth, layout.preferredHeight);
                if (size > 0f)
                {
                    layout.minWidth = size;
                    layout.minHeight = size;
                    layout.preferredWidth = size;
                    layout.preferredHeight = size;
                    layout.flexibleWidth = 0f;
                    layout.flexibleHeight = 0f;
                    GP_LayoutSquare.Lock(this, size);
                }
            }

            if (Target != null && Target.sprite != null)
                Target.preserveAspect = true;
        }

        public void Load(string url, Sprite fallback = null)
        {
            if (fallback != null)
                placeholder = fallback;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            url = Normalize(url);
            _url = url;
            if (string.IsNullOrEmpty(url))
            {
                Apply(placeholder);
                return;
            }
            if (Cache.TryGetValue(url, out var cached) && cached != null)
            {
                Apply(cached);
                return;
            }

            Apply(placeholder);
            if (isActiveAndEnabled)
                _routine = StartCoroutine(Download(url));
        }

        void OnDisable()
        {
            if (_routine == null)
                return;
            StopCoroutine(_routine);
            _routine = null;
        }

        IEnumerator Download(string url)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            request.timeout = 15;
            request.redirectLimit = 16;
            request.SetRequestHeader("Accept", "image/*,*/*");
            yield return request.SendWebRequest();
            _routine = null;

            if (_url != url)
                yield break;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Apply(placeholder);
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                Apply(placeholder);
                yield break;
            }

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            Store(url, sprite);
            Apply(sprite);
        }

        void Apply(Sprite sprite)
        {
            if (Target == null)
                return;
            var empty = sprite == null && placeholder == null;
            Target.enabled = true;
            Target.sprite = sprite != null ? sprite : placeholder;
            Target.color = Target.sprite != null ? Color.white : EmptyFill;
            Target.preserveAspect = Target.sprite != null;
            if (empty)
                Target.type = Image.Type.Simple;
        }

        static string Normalize(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";
            url = url.Trim();
            if (url.StartsWith("//"))
                url = "https:" + url;
            return GP_Images.FormatToPng(url);
        }

        static void Store(string url, Sprite sprite)
        {
            if (Cache.ContainsKey(url))
                return;
            Cache[url] = sprite;
            CacheOrder.Add(url);
            while (CacheOrder.Count > CacheLimit)
            {
                var oldest = CacheOrder[0];
                CacheOrder.RemoveAt(0);
                Cache.Remove(oldest);
            }
        }
    }
}
