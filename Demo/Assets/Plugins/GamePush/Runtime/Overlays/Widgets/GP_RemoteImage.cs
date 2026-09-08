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

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly List<string> CacheOrder = new List<string>();

        public Sprite placeholder;

        Image _image;
        string _url;
        Coroutine _routine;

        Image Target => _image != null ? _image : _image = GetComponent<Image>();

        public void Load(string url, Sprite fallback = null)
        {
            if (fallback != null)
                placeholder = fallback;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

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
            yield return request.SendWebRequest();
            _routine = null;

            if (request.result != UnityWebRequest.Result.Success || _url != url)
                yield break;

            var texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
                yield break;

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            Store(url, sprite);
            Apply(sprite);
        }

        void Apply(Sprite sprite)
        {
            if (Target == null)
                return;
            Target.sprite = sprite;
            Target.enabled = sprite != null;
            Target.preserveAspect = true;
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
