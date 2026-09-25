using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace SortThem
{
    public class ScreenshotCapturer : MonoBehaviour
    {
        public enum ShotResolution
        {
            FullHD,
            QuadHD,
            UltraHD
        }

        [Header("Съёмка")]
        [SerializeField] private ShotResolution _resolution = ShotResolution.UltraHD;

        [Tooltip("Пусто — берётся Camera.main.")]
        [SerializeField] private Camera _sourceCamera;

        [Tooltip("Слои, которых не будет в кадре. По умолчанию UI.")]
        [SerializeField] private LayerMask _excludedLayers = 1 << 5;

        [Tooltip("Убирать из стека overlay-камеры, которые рисуют исключённые слои.")]
        [SerializeField] private bool _excludeOverlayCameras = true;

        [Tooltip("Прозрачный фон вместо неба. Небо и туман в кадр не попадут.")]
        [SerializeField] private bool _transparentBackground;

    #if UNITY_EDITOR
        [Header("Клавиша (работает только в редакторе)")]
        [SerializeField] private Key _hotkey = Key.U;
    #endif

        [Header("Файл")]
        [Tooltip("В редакторе — папка рядом с Assets, в билде — persistentDataPath.")]
        [SerializeField] private string _folderName = "Screenshots";

        [Tooltip("Префикс имени файла.")]
        [SerializeField] private string _filePrefix = "SortThem";

        private bool _isCapturing;

    #if UNITY_EDITOR
        private void Update()
        {
            if (_isCapturing || _hotkey == Key.None) return;
            if (Keyboard.current == null) return;
            if (!Keyboard.current[_hotkey].wasPressedThisFrame) return;

            Capture();
        }
    #endif

        [ContextMenu("Сделать скриншот")]
        public void Capture()
        {
            if (_isCapturing) return;

            var camera = _sourceCamera != null ? _sourceCamera : Camera.main;

            if (camera == null)
            {
                Debug.LogWarning("ScreenshotCapturer: камера не найдена — назначь Source Camera или поставь тег MainCamera.");
                return;
            }

            var size = GetSize();
            var stackBackup = new List<KeyValuePair<int, Camera>>();
            var cameraStack = GetStack(camera);

            var previousTarget = camera.targetTexture;
            var previousMask = camera.cullingMask;
            var previousClearFlags = camera.clearFlags;
            var previousBackground = camera.backgroundColor;
            var previousAspect = camera.aspect;

            RenderTexture renderTexture = null;
            Texture2D texture = null;
            _isCapturing = true;

            try
            {
                if (_excludeOverlayCameras && cameraStack != null)
                {
                    for (var i = cameraStack.Count - 1; i >= 0; i--)
                    {
                        var overlay = cameraStack[i];
                        if (overlay == null) continue;
                        if ((overlay.cullingMask & _excludedLayers.value) == 0) continue;

                        stackBackup.Add(new KeyValuePair<int, Camera>(i, overlay));
                        cameraStack.RemoveAt(i);
                    }
                }

                var descriptor = new RenderTextureDescriptor(size.x, size.y, RenderTextureFormat.ARGB32, 24)
                {
                    msaaSamples = Mathf.Max(1, QualitySettings.antiAliasing),
                    sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
                    useMipMap = false,
                    autoGenerateMips = false
                };

                renderTexture = RenderTexture.GetTemporary(descriptor);

                camera.targetTexture = renderTexture;
                camera.cullingMask = previousMask & ~_excludedLayers.value;
                camera.aspect = size.x / (float)size.y;

                if (_transparentBackground)
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                }

                camera.Render();

                var previousActive = RenderTexture.active;
                RenderTexture.active = renderTexture;

                texture = new Texture2D(size.x, size.y, _transparentBackground ? TextureFormat.ARGB32 : TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0f, 0f, size.x, size.y), 0, 0);
                texture.Apply();

                RenderTexture.active = previousActive;

                Save(texture, size);
            }
            catch (Exception exception)
            {
                Debug.LogError("ScreenshotCapturer: " + exception.Message);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.cullingMask = previousMask;
                camera.clearFlags = previousClearFlags;
                camera.backgroundColor = previousBackground;

                if (Mathf.Approximately(previousAspect, size.x / (float)size.y)) camera.aspect = previousAspect;
                else camera.ResetAspect();

                for (var i = stackBackup.Count - 1; i >= 0; i--)
                    cameraStack.Insert(stackBackup[i].Key, stackBackup[i].Value);

                if (renderTexture != null) RenderTexture.ReleaseTemporary(renderTexture);

                if (texture != null)
                {
                    if (Application.isPlaying) Destroy(texture);
                    else DestroyImmediate(texture);
                }

                _isCapturing = false;
            }
        }

        private void Save(Texture2D texture, Vector2Int size)
        {
            var directory = GetDirectory();
            Directory.CreateDirectory(directory);

            var fileName = string.Format("{0}_{1}x{2}_{3}.png", _filePrefix, size.x, size.y, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            var fullPath = Path.Combine(directory, fileName);

            File.WriteAllBytes(fullPath, texture.EncodeToPNG());

            Debug.Log("Скриншот сохранён: " + fullPath);
        }

        private string GetDirectory()
        {
    #if UNITY_EDITOR
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, _folderName);
    #else
            return Path.Combine(Application.persistentDataPath, _folderName);
    #endif
        }

        private static List<Camera> GetStack(Camera camera)
        {
            var data = camera.GetComponent<UniversalAdditionalCameraData>();

            if (data == null || data.renderType != CameraRenderType.Base) return null;

            return data.cameraStack;
        }

        private Vector2Int GetSize()
        {
            switch (_resolution)
            {
                case ShotResolution.FullHD: return new Vector2Int(1920, 1080);
                case ShotResolution.QuadHD: return new Vector2Int(2560, 1440);
                default: return new Vector2Int(3840, 2160);
            }
        }
    }
}
